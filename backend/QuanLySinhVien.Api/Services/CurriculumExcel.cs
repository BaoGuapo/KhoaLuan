using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using QuanLySinhVien.Api.Contracts.Training;

namespace QuanLySinhVien.Api.Services;

public sealed record ExcelPreview(IReadOnlyList<CurriculumCourseRequest> MonHocs, IReadOnlyList<string> Errors);

// Mẫu XLSX nhỏ, chỉ đọc giá trị (không thực thi công thức/macro/liên kết).
public static class CurriculumExcel
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    public static ExcelPreview Read(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > 200 || archive.Entries.Sum(x => x.Length) > 10 * 1024 * 1024)
            throw new InvalidDataException("File Excel vượt giới hạn dữ liệu giải nén.");
        var shared = archive.GetEntry("xl/sharedStrings.xml") is { } strings
            ? Load(strings).Descendants(Ns + "si").Select(x => string.Concat(x.Descendants(Ns + "t").Select(t => t.Value))).ToArray()
            : [];
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException("Không tìm thấy sheet đầu tiên theo mẫu.");
        var rows = Load(sheet).Descendants(Ns + "row").ToArray();
        if (rows.Length is < 2 or > 201) throw new InvalidDataException("File cần tiêu đề và từ 1 đến 200 dòng môn học.");
        string Value(XElement cell)
        {
            if (cell.Element(Ns + "f") is not null) throw new InvalidDataException("Không dùng công thức trong file nhập.");
            var value = cell.Element(Ns + "v")?.Value ?? string.Concat(cell.Descendants(Ns + "t").Select(t => t.Value));
            if ((string?)cell.Attribute("t") == "s")
                return int.TryParse(value, out var index) && index >= 0 && index < shared.Length ? shared[index].Trim() : throw new InvalidDataException("Chuỗi Excel không hợp lệ.");
            return value.Trim();
        }
        string[] Cells(XElement row)
        {
            var values = new string[3]; Array.Fill(values, "");
            foreach (var cell in row.Elements(Ns + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? "";
                var column = new string(reference.TakeWhile(char.IsLetter).ToArray());
                var value = Value(cell);
                var index = column switch { "A" => 0, "B" => 1, "C" => 2, _ => -1 };
                if (index >= 0) values[index] = value;
                else if (value.Length > 0) throw new InvalidDataException("File chỉ gồm 3 cột A–C theo mẫu.");
            }
            return values;
        }
        if (!Cells(rows[0]).SequenceEqual(new[] { "MaMonHoc", "ThuTuHocKy", "BatBuoc" }))
            throw new InvalidDataException("Tiêu đề phải là MaMonHoc, ThuTuHocKy, BatBuoc. Hãy tải file mẫu.");
        var courses = new List<CurriculumCourseRequest>(); var errors = new List<string>();
        for (var i = 1; i < rows.Length; i++)
        {
            var cells = Cells(rows[i]);
            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            if (cells[0].Length is < 1 or > 50 || !int.TryParse(cells[1], out var semester) || semester is < 1 or > 12 || cells[2] is not ("0" or "1"))
            { errors.Add($"Dòng {(string?)rows[i].Attribute("r") ?? (i + 1).ToString()}: mã môn, học kỳ (1–12) hoặc bắt buộc (0/1) không hợp lệ."); continue; }
            courses.Add(new(cells[0], semester, cells[2] == "1"));
        }
        if (TrainingRules.ValidateCourses(courses) is { } error) errors.Add(error);
        return new(courses, errors);
    }
    private static XDocument Load(ZipArchiveEntry entry)
    {
        if (entry.Length > 5 * 1024 * 1024) throw new InvalidDataException("Sheet vượt giới hạn.");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 5 * 1024 * 1024 });
        return XDocument.Load(reader);
    }
    public static byte[] Template()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Write(string path, string text)
            { using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false)); writer.Write(text); }
            Write("[Content_Types].xml", """
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            Write("_rels/.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Write("xl/workbook.xml", """
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="ChuongTrinh" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            Write("xl/_rels/workbook.xml.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            Write("xl/worksheets/sheet1.xml", """
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>MaMonHoc</t></is></c><c r="B1" t="inlineStr"><is><t>ThuTuHocKy</t></is></c><c r="C1" t="inlineStr"><is><t>BatBuoc</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>THAY_BANG_MA_MON</t></is></c><c r="B2"><v>1</v></c><c r="C2"><v>1</v></c></row></sheetData></worksheet>
                """);
        }
        return stream.ToArray();
    }
}
