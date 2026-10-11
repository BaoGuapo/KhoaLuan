using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySinhVien.Api.Data;

namespace QuanLySinhVien.Api.Services;

public sealed class FaceRecognitionOptions
{
    public const string SectionName = "FaceRecognition";
    public int DescriptorLength { get; set; } = 512;
    public double MatchThreshold { get; set; } = 0.35;
    public double AmbiguityMargin { get; set; } = 0.04;
    public double SampleConsistencyThreshold { get; set; } = 0.70;
}

public sealed record FaceServiceResult<T>(T? Value, string? ErrorCode = null, string? ErrorMessage = null)
{
    public bool Succeeded => ErrorCode is null;
    public static FaceServiceResult<T> Success(T value) => new(value);
    public static FaceServiceResult<T> Failure(string code, string message) => new(default, code, message);
}

public sealed record FaceMatch(string MaSinhVien, string HoTen, double Similarity);

public interface IFaceRecognitionService
{
    FaceServiceResult<double[]> NormalizeSamples(IReadOnlyCollection<double[]> samples);
    string ProtectDescriptor(double[] descriptor);
    bool TryUnprotectDescriptor(string protectedValue, out double[] descriptor);
    Task<FaceServiceResult<FaceMatch>> MatchStudentInClassAsync(
        string maLopHocPhan,
        double[] descriptor,
        CancellationToken ct);
    Task<bool> VerifyStudentAsync(string maSinhVien, double[] descriptor, CancellationToken ct);
}

public sealed class FaceRecognitionService(
    AppDbContext db,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<FaceRecognitionOptions> options) : IFaceRecognitionService
{
    private const string ProtectedPrefix = "dp:v2:mobilenetv2_mcp:";
    private readonly FaceRecognitionOptions _options = options.Value;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "QuanLySinhVien.FaceDescriptor.v1");

    public FaceServiceResult<double[]> NormalizeSamples(IReadOnlyCollection<double[]> samples)
    {
        if (samples.Count is < 3 or > 5)
            return FaceServiceResult<double[]>.Failure(
                "InvalidSampleCount",
                "Cần từ 3 đến 5 mẫu khuôn mặt liên tiếp.");

        foreach (var sample in samples)
        {
            if (sample is null || sample.Length != _options.DescriptorLength
                || sample.Any(value => !double.IsFinite(value) || Math.Abs(value) > 1.0001)
                || Math.Abs(VectorNorm(sample) - 1) > 0.05)
            {
                return FaceServiceResult<double[]>.Failure(
                    "InvalidDescriptor",
                    $"Mỗi mẫu khuôn mặt phải có đúng {_options.DescriptorLength} giá trị hợp lệ.");
            }
        }

        var reference = samples.First();
        if (samples.Any(sample => CosineSimilarity(sample, reference) < _options.SampleConsistencyThreshold))
        {
            return FaceServiceResult<double[]>.Failure(
                "InconsistentSamples",
                "Các mẫu không đủ nhất quán. Hãy giữ một khuôn mặt trong khung hình và thử lại.");
        }

        var average = new double[_options.DescriptorLength];
        foreach (var sample in samples)
        {
            for (var index = 0; index < average.Length; index++) average[index] += sample[index];
        }
        for (var index = 0; index < average.Length; index++) average[index] /= samples.Count;

        var norm = VectorNorm(average);
        if (norm < 0.001)
            return FaceServiceResult<double[]>.Failure("InvalidDescriptor", "Không thể chuẩn hóa mẫu khuôn mặt.");
        for (var index = 0; index < average.Length; index++) average[index] /= norm;

        return FaceServiceResult<double[]>.Success(average);
    }

    public string ProtectDescriptor(double[] descriptor)
    {
        var json = JsonSerializer.Serialize(descriptor);
        return ProtectedPrefix + _protector.Protect(json);
    }

    public bool TryUnprotectDescriptor(string protectedValue, out double[] descriptor)
    {
        descriptor = [];
        try
        {
            if (!protectedValue.StartsWith(ProtectedPrefix, StringComparison.Ordinal)) return false;
            var json = _protector.Unprotect(protectedValue[ProtectedPrefix.Length..]);
            var parsed = JsonSerializer.Deserialize<double[]>(json);
            if (parsed is null || parsed.Length != _options.DescriptorLength
                || parsed.Any(value => !double.IsFinite(value))
                || Math.Abs(VectorNorm(parsed) - 1) > 0.05) return false;
            descriptor = parsed;
            return true;
        }
        catch (Exception) when (protectedValue.Length > 0)
        {
            return false;
        }
    }

    public async Task<FaceServiceResult<FaceMatch>> MatchStudentInClassAsync(
        string maLopHocPhan,
        double[] descriptor,
        CancellationToken ct)
    {
        var candidates = await db.DanhSachSinhVienLops.AsNoTracking()
            .Where(x => x.MaLopHocPhan == maLopHocPhan)
            .Join(
                db.HoSoKhuonMats.AsNoTracking(),
                enrollment => enrollment.MaSinhVien,
                profile => profile.MaSinhVien,
                (enrollment, profile) => new
                {
                    enrollment.MaSinhVien,
                    enrollment.SinhVien.HoTen,
                    profile.VectorJson
                })
            .ToListAsync(ct);

        var matches = new List<FaceMatch>();
        foreach (var candidate in candidates)
        {
            if (!TryUnprotectDescriptor(candidate.VectorJson, out var savedDescriptor)) continue;
            matches.Add(new FaceMatch(
                candidate.MaSinhVien,
                candidate.HoTen,
                CosineSimilarity(descriptor, savedDescriptor)));
        }

        var ordered = matches.OrderByDescending(x => x.Similarity).Take(2).ToArray();
        if (ordered.Length == 0 || ordered[0].Similarity < _options.MatchThreshold)
            return FaceServiceResult<FaceMatch>.Failure(
                "FaceNotMatched",
                "Không nhận diện được sinh viên thuộc lớp học phần này.");
        if (ordered.Length > 1 && ordered[0].Similarity - ordered[1].Similarity < _options.AmbiguityMargin)
            return FaceServiceResult<FaceMatch>.Failure(
                "AmbiguousMatch",
                "Kết quả nhận diện chưa đủ rõ ràng. Hãy điều chỉnh ánh sáng và thử lại.");

        return FaceServiceResult<FaceMatch>.Success(ordered[0]);
    }

    public async Task<bool> VerifyStudentAsync(string maSinhVien, double[] descriptor, CancellationToken ct)
    {
        var protectedValue = await db.HoSoKhuonMats.AsNoTracking()
            .Where(x => x.MaSinhVien == maSinhVien)
            .Select(x => x.VectorJson)
            .SingleOrDefaultAsync(ct);
        return protectedValue is not null
            && TryUnprotectDescriptor(protectedValue, out var savedDescriptor)
            && CosineSimilarity(descriptor, savedDescriptor) >= _options.MatchThreshold;
    }

    private static double CosineSimilarity(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        var sum = 0d;
        for (var index = 0; index < left.Count; index++)
            sum += left[index] * right[index];
        return sum / (VectorNorm(left) * VectorNorm(right));
    }

    private static double VectorNorm(IReadOnlyList<double> values)
    {
        var squares = 0d;
        for (var index = 0; index < values.Count; index++) squares += values[index] * values[index];
        return Math.Sqrt(squares);
    }
}
