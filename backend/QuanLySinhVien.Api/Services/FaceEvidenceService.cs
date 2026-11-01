using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace QuanLySinhVien.Api.Services;

public sealed class FaceEvidenceOptions
{
    public string ModelDirectory { get; set; } = "Models/Face";
    public double MinimumLiveScore { get; set; } = 0.80;
    public double MinimumTurn { get; set; } = 0.12;
}

public sealed class FaceEvidenceException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public interface IFaceEvidenceService
{
    Task<IReadOnlyCollection<double[]>> AnalyzeAsync(string[] frames, string direction, CancellationToken ct);
    Task<FacePoseObservation> InspectPoseAsync(string frame, CancellationToken ct);
}

public sealed record FacePoseObservation(bool Usable, string Code, string Message, double? Pose, double MinimumTurn);

// Only server-derived detections, PAD decisions and embeddings authorize attendance.
// Never accept bounding boxes, embeddings, pose or isLive supplied by the caller.
public sealed class FaceEvidenceService : IFaceEvidenceService, IDisposable
{
    private readonly Lazy<(InferenceSession Detector, InferenceSession Recognition, InferenceSession Pad)> _models;
    private readonly FaceEvidenceOptions _options;
    private readonly SemaphoreSlim _gate = new(2);
    public FaceEvidenceService(IHostEnvironment environment, IOptions<FaceEvidenceOptions> options)
    {
        _options = options.Value;
        _models = new(() =>
        {
            var path = Path.GetFullPath(_options.ModelDirectory, environment.ContentRootPath);
            var files = new[] { "det_2.5g.onnx", "mobilenetv2_mcp.onnx", "MiniFASNetV2.onnx" };
            if (files.Any(f => !File.Exists(Path.Combine(path, f))))
                throw new FaceEvidenceException("AiServiceUnavailable", "Máy chủ chưa có đủ mô hình xác minh khuôn mặt.");
            using var config = new Microsoft.ML.OnnxRuntime.SessionOptions { IntraOpNumThreads = 2, InterOpNumThreads = 1 };
            return (new(Path.Combine(path, files[0]), config), new(Path.Combine(path, files[1]), config),
                new(Path.Combine(path, files[2]), config));
        });
    }

    public async Task<IReadOnlyCollection<double[]>> AnalyzeAsync(string[] frames, string direction, CancellationToken ct)
    {
        if (frames is null || frames.Length != 3 || frames.Any(x => string.IsNullOrEmpty(x) || x.Length > 1_400_000))
            throw new FaceEvidenceException("InvalidEvidence", "Cần ba ảnh camera, mỗi ảnh tối đa 1 MB.");
        if (frames.Distinct(StringComparer.Ordinal).Count() != 3)
            throw new FaceEvidenceException("LivenessFailed", "Các ảnh trùng nhau. Hãy thực hiện lại hướng dẫn trước camera.");
        await _gate.WaitAsync(ct);
        try
        {
            var models = _models.Value;
            var descriptors = new List<double[]>();
            var poses = new List<double>();
            foreach (var frame in frames)
            {
                ct.ThrowIfCancellationRequested();
                using var image = Decode(frame);
                var faces = Detect(models.Detector, image);
                if (faces.Count != 1)
                    throw new FaceEvidenceException(faces.Count == 0 ? "NoFaceDetected" : "MultipleFaces",
                        faces.Count == 0 ? "Không tìm thấy khuôn mặt rõ ràng." : "Chỉ để một khuôn mặt trong khung hình.");
                var face = faces[0];
                if (face.Width < 80 || face.Height < 80 || face.Points.Any(p => !double.IsFinite(p.X + p.Y)))
                    throw new FaceEvidenceException("ImageQualityTooLow", "Đưa mặt gần camera hơn và tăng ánh sáng.");
                var live = LiveScore(models.Pad, image, face);
                if (!double.IsFinite(live) || live < _options.MinimumLiveScore)
                    throw new FaceEvidenceException("SpoofDetected", "Chưa xác minh được người thật. Không dùng ảnh hoặc video trước camera.");
                poses.Add(Pose(face));
                descriptors.Add(Embedding(models.Recognition, image, face.Points));
            }
            ValidateMotion(poses, direction, _options.MinimumTurn);
            return descriptors;
        }
        catch (FaceEvidenceException) { throw; }
        catch (Exception e) when (e is OnnxRuntimeException or FileNotFoundException or DllNotFoundException or TypeInitializationException)
        {
            throw new FaceEvidenceException("AiServiceUnavailable", "Dịch vụ xác minh khuôn mặt chưa sẵn sàng.");
        }
        finally { _gate.Release(); }
    }

    // Feedback only: this never consumes a challenge or authorizes enrollment.
    // The final three exact frames still pass the complete evidence pipeline.
    public async Task<FacePoseObservation> InspectPoseAsync(string frame, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var models = _models.Value;
            using var image = Decode(frame);
            var detections = Detect(models.Detector, image);
            if (detections.Count != 1)
                return new(false, detections.Count == 0 ? "NoFaceDetected" : "MultipleFaces",
                    detections.Count == 0 ? "Đưa khuôn mặt vào khung oval." : "Chỉ để một người trong khung hình.", null, _options.MinimumTurn);
            var face = detections[0];
            if (face.Width < 80 || face.Height < Math.Max(80, image.Height * .25))
                return new(false, "TooFar", "Đưa mặt gần camera hơn.", null, _options.MinimumTurn);
            var cx = (face.X + face.Width / 2) / image.Width;
            var cy = (face.Y + face.Height / 2) / image.Height;
            if (cx is < .25 or > .75 || cy is < .20 or > .80)
                return new(false, "OffCenter", "Đưa mặt vào giữa khung oval.", null, _options.MinimumTurn);
            var eyes = face.Points;
            if (Math.Abs(eyes[1].Y - eyes[0].Y) > Math.Abs(eyes[1].X - eyes[0].X) * .35)
                return new(false, "Tilted", "Giữ đầu thẳng, không nghiêng sang vai.", null, _options.MinimumTurn);
            var pose = Pose(face);
            var live = LiveScore(models.Pad, image, face);
            if (!double.IsFinite(live) || live < _options.MinimumLiveScore)
                return new(false, "SpoofDetected", "Chưa xác minh được người thật. Giữ đủ ánh sáng và nhìn trực tiếp vào camera.", null, _options.MinimumTurn);
            return new(true, "FaceReady", "Đã thấy khuôn mặt rõ ràng.", pose, _options.MinimumTurn);
        }
        catch (FaceEvidenceException e) when (e.Code != "AiServiceUnavailable")
        { return new(false, e.Code, e.Message, null, _options.MinimumTurn); }
        catch (Exception e) when (e is OnnxRuntimeException or FileNotFoundException or DllNotFoundException or TypeInitializationException)
        { throw new FaceEvidenceException("AiServiceUnavailable", "Dịch vụ xác minh khuôn mặt chưa sẵn sàng."); }
        finally { _gate.Release(); }
    }

    private static double Pose(Detection face)
    {
        if (face.Points.Any(p => !double.IsFinite(p.X + p.Y)))
            throw new FaceEvidenceException("ImageQualityTooLow", "Khuôn mặt chưa đủ rõ.");
        var left = face.Points[0]; var right = face.Points[1]; var nose = face.Points[2];
        var dx = right.X - left.X; var dy = right.Y - left.Y;
        var length2 = dx * dx + dy * dy;
        if (length2 < 100) throw new FaceEvidenceException("ImageQualityTooLow", "Khuôn mặt chưa đủ rõ.");
        return ((nose.X - left.X) * dx + (nose.Y - left.Y) * dy) / length2 - 0.5;
    }

    public static void ValidateMotion(IReadOnlyList<double> poses, string direction, double minimumTurn = 0.12)
    {
        if (poses.Count != 3 || poses.Any(x => !double.IsFinite(x)) || direction is not ("left" or "right")
            || Math.Abs(poses[0]) > 0.30 || Math.Abs(poses[2] - poses[0]) > 0.16
            || (poses[1] - poses[0]) * (direction == "left" ? 1 : -1) < minimumTurn)
            throw new FaceEvidenceException("LivenessFailed", "Chưa đúng động tác. Nhìn thẳng, quay theo hướng yêu cầu, rồi nhìn thẳng lại.");
    }

    private static FaceImage Decode(string frame)
    {
        try
        {
            var bytes = Convert.FromBase64String(frame);
            if (bytes.Length < 4 || bytes.Length > 1_050_000 || bytes[0] != 255 || bytes[1] != 216)
                throw new FormatException();
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec is null) throw new FormatException();
            var info = codec.Info;
            if (info.Width < 160 || info.Height < 160 || info.Width > 2048 || info.Height > 2048)
                throw new FormatException();
            return FaceImage.Decode(codec);
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        { throw new FaceEvidenceException("InvalidImage", "Ảnh phải là JPEG hợp lệ, kích thước 160–2048 pixel và tối đa 1 MB."); }
    }

    private readonly record struct XY(double X, double Y);
    private sealed record Detection(double X, double Y, double Width, double Height, double Score, XY[] Points);

    private static List<Detection> Detect(InferenceSession model, FaceImage image)
    {
        const int size = 640;
        var scale = Math.Min((double)size / image.Width, (double)size / image.Height);
        using var resized = image.Resize(Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)));
        var input = new DenseTensor<float>(new[] { 1, 3, size, size });
        // SCRFD uses RGB, mean 127.5, divisor 128; padding is a black pixel.
        input.Buffer.Span.Fill(-127.5f / 128);
        for (var y = 0; y < resized.Height; y++)
        for (var x = 0; x < resized.Width; x++)
        {
            var p = resized[x, y];
            input[0, 0, y, x] = (p.Red - 127.5f) / 128;
            input[0, 1, y, x] = (p.Green - 127.5f) / 128;
            input[0, 2, y, x] = (p.Blue - 127.5f) / 128;
        }
        using var outputs = model.Run([NamedOnnxValue.CreateFromTensor(model.InputMetadata.Keys.First(), input)]);
        var values = outputs.Select(x => x.AsTensor<float>().ToArray()).ToArray();
        if (values.Length != 9) throw new FaceEvidenceException("AiServiceUnavailable", "Mô hình phát hiện mặt không tương thích.");
        var candidates = new List<Detection>();
        for (var level = 0; level < 3; level++)
        {
            var stride = 8 << level; var columns = size / stride;
            var scores = values[level]; var boxes = values[level + 3]; var keypoints = values[level + 6];
            for (var i = 0; i < scores.Length; i++)
            {
                if (scores[i] < 0.65f) continue;
                var cx = i / 2 % columns * stride; var cy = i / 2 / columns * stride;
                var x1 = (cx - boxes[i * 4] * stride) / scale;
                var y1 = (cy - boxes[i * 4 + 1] * stride) / scale;
                var x2 = (cx + boxes[i * 4 + 2] * stride) / scale;
                var y2 = (cy + boxes[i * 4 + 3] * stride) / scale;
                var points = Enumerable.Range(0, 5).Select(k => new XY(
                    (cx + keypoints[i * 10 + k * 2] * stride) / scale,
                    (cy + keypoints[i * 10 + k * 2 + 1] * stride) / scale)).ToArray();
                candidates.Add(new(x1, y1, x2 - x1, y2 - y1, scores[i], points));
            }
        }
        var selected = new List<Detection>();
        foreach (var candidate in candidates.OrderByDescending(x => x.Score))
            if (selected.All(x => Intersection(x, candidate) < 0.4)) selected.Add(candidate);
        return selected;
    }

    private static double Intersection(Detection a, Detection b)
    {
        var area = Math.Max(0, Math.Min(a.X + a.Width, b.X + b.Width) - Math.Max(a.X, b.X))
            * Math.Max(0, Math.Min(a.Y + a.Height, b.Y + b.Height) - Math.Max(a.Y, b.Y));
        return area / Math.Max(1, a.Width * a.Height + b.Width * b.Height - area);
    }

    private static double LiveScore(InferenceSession model, FaceImage image, Detection face)
    {
        var scale = Math.Min(2.7, Math.Min((image.Width - 1) / face.Width, (image.Height - 1) / face.Height));
        var x1 = Math.Max(0, (int)(face.X + face.Width / 2 - face.Width * scale / 2));
        var y1 = Math.Max(0, (int)(face.Y + face.Height / 2 - face.Height * scale / 2));
        var x2 = Math.Min(image.Width - 1, (int)(face.X + face.Width / 2 + face.Width * scale / 2));
        var y2 = Math.Min(image.Height - 1, (int)(face.Y + face.Height / 2 + face.Height * scale / 2));
        if (x2 <= x1 || y2 <= y1) return 0;
        using var crop = image.Resize(80, 80, new SKRect(x1, y1, x2 + 1, y2 + 1));
        var input = new DenseTensor<float>(new[] { 1, 3, 80, 80 });
        for (var y = 0; y < 80; y++) for (var x = 0; x < 80; x++)
        {
            var p = crop[x, y]; // MiniFASNet expects BGR in the original 0..255 range.
            input[0, 0, y, x] = p.Blue; input[0, 1, y, x] = p.Green; input[0, 2, y, x] = p.Red;
        }
        using var output = model.Run([NamedOnnxValue.CreateFromTensor(model.InputMetadata.Keys.First(), input)]);
        var logits = output.First().AsTensor<float>().ToArray();
        if (logits.Length != 3) return 0;
        var max = logits.Max(); var sum = logits.Sum(x => Math.Exp(x - max));
        return Math.Exp(logits[1] - max) / sum;
    }

    private static double[] Embedding(InferenceSession model, FaceImage image, XY[] source)
    {
        XY[] target = [new(38.2946, 51.6963), new(73.5318, 51.5014), new(56.0252, 71.7366), new(41.5493, 92.3655), new(70.7299, 92.2041)];
        var sx = source.Average(x => x.X); var sy = source.Average(x => x.Y);
        var tx = target.Average(x => x.X); var ty = target.Average(x => x.Y);
        double denominator = 0, real = 0, imaginary = 0;
        for (var i = 0; i < 5; i++)
        {
            var x = source[i].X - sx; var y = source[i].Y - sy;
            var u = target[i].X - tx; var v = target[i].Y - ty;
            denominator += x * x + y * y; real += x * u + y * v; imaginary += x * v - y * u;
        }
        var a = real / denominator; var b = imaginary / denominator;
        var offsetX = tx - a * sx + b * sy; var offsetY = ty - b * sx - a * sy;
        var det = a * a + b * b;
        if (!double.IsFinite(det) || det < 0.000001) throw new FaceEvidenceException("ImageQualityTooLow", "Không thể căn chỉnh mặt.");
        var input = new DenseTensor<float>(new[] { 1, 3, 112, 112 });
        for (var y = 0; y < 112; y++) for (var x = 0; x < 112; x++)
        {
            var px = (a * (x - offsetX) + b * (y - offsetY)) / det;
            var py = (-b * (x - offsetX) + a * (y - offsetY)) / det;
            for (var c = 0; c < 3; c++) input[0, c, y, x] = (Sample(image, px, py, c) - 127.5f) / 127.5f;
        }
        using var output = model.Run([NamedOnnxValue.CreateFromTensor(model.InputMetadata.Keys.First(), input)]);
        var raw = output.First().AsTensor<float>().Select(x => (double)x).ToArray();
        var norm = Math.Sqrt(raw.Sum(x => x * x));
        if (raw.Length != 512 || !double.IsFinite(norm) || norm < 0.001)
            throw new FaceEvidenceException("AiServiceUnavailable", "Mô hình nhận diện không tương thích.");
        return raw.Select(x => x / norm).ToArray();
    }

    private static float Sample(FaceImage image, double x, double y, int channel)
    {
        if (x < 0 || y < 0 || x >= image.Width - 1 || y >= image.Height - 1) return 0;
        var ix = (int)x; var iy = (int)y; var fx = (float)(x - ix); var fy = (float)(y - iy);
        float Get(int xx, int yy) { var p = image[xx, yy]; return channel == 0 ? p.Red : channel == 1 ? p.Green : p.Blue; }
        return (Get(ix, iy) * (1 - fx) + Get(ix + 1, iy) * fx) * (1 - fy)
            + (Get(ix, iy + 1) * (1 - fx) + Get(ix + 1, iy + 1) * fx) * fy;
    }

    public void Dispose()
    {
        if (_models.IsValueCreated) { _models.Value.Detector.Dispose(); _models.Value.Recognition.Dispose(); _models.Value.Pad.Dispose(); }
        _gate.Dispose();
    }
}
