using System.IO.Compression;
using System.Text;
using System.Xml;
using QuanLySinhVien.Api.Contracts.Reporting;

namespace QuanLySinhVien.Api.Services;

/// <summary>
/// Ghi báo cáo điểm danh ra tệp XLSX bằng ZIP và XML thuần, cùng cách
/// <see cref="CurriculumExcel"/> đọc tệp, nên không cần thêm thư viện ngoài.
/// Chuỗi được ghi trực tiếp vào ô (inline string) để khỏi dựng bảng sharedStrings.
/// </summary>
public static class AttendanceReportExcel
{
    private sealed record Sheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<object?[]> Rows);

    public static byte[] Write(AttendanceReportResponse report)
    {
        var sheets = new List<Sheet>
        {
            new("Tong quan", ["Chỉ tiêu", "Giá trị"], Overview(report)),
            new("Theo mon hoc",
                ["Mã môn học", "Tên môn học", "Số lớp", "Số sinh viên", "Có mặt", "Đi trễ", "Có phép", "Vắng", "Chưa điểm danh", "Lượt dự kiến", "Tỷ lệ chuyên cần"],
                report.TheoMonHoc.Select(x => new object?[]
                {
                    x.MaMonHoc, x.TenMonHoc, x.SoLop, x.SoSinhVien,
                    x.CoMat, x.DiTre, x.CoPhep, x.Vang, x.ChuaDiemDanh, x.SoLuotDuKien,
                    // Ô để trống khi chưa có lượt dự kiến, tránh hiểu nhầm là chuyên cần 0%.
                    x.SoLuotDuKien == 0 ? null : x.TyLeChuyenCan
                }).ToList()),
            new("Theo lop hoc phan",
                ["Mã lớp học phần", "Mã môn học", "Tên môn học", "Giảng viên", "Sĩ số", "Số buổi đã diễn ra", "Có mặt", "Đi trễ", "Có phép", "Vắng", "Chưa điểm danh", "Lượt dự kiến", "Tỷ lệ chuyên cần", "SV cảnh báo"],
                report.TheoLopHocPhan.Select(x => new object?[]
                {
                    x.MaLopHocPhan, x.MaMonHoc, x.TenMonHoc, x.HoTenGiangVien, x.SiSo, x.SoBuoiDaDienRa,
                    x.CoMat, x.DiTre, x.CoPhep, x.Vang, x.ChuaDiemDanh, x.SoLuotDuKien,
                    x.SoLuotDuKien == 0 ? null : x.TyLeChuyenCan,
                    x.SoSinhVienCanhBao
                }).ToList()),
            new("Theo ngay",
                ["Ngày học", "Số buổi", "Lượt dự kiến", "Có mặt và đi trễ", "Tỷ lệ chuyên cần"],
                report.TheoNgay.Select(x => new object?[]
                {
                    x.Ngay.ToString("dd/MM/yyyy"), x.SoBuoi, x.SoLuotDuKien, x.CoMatVaDiTre, x.TyLeChuyenCan
                }).ToList())
        };

        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "[Content_Types].xml", ContentTypes(sheets.Count));
            Add(archive, "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Add(archive, "xl/workbook.xml", Workbook(sheets));
            Add(archive, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Count));
            Add(archive, "xl/styles.xml", Styles());
            for (var i = 0; i < sheets.Count; i++)
                Add(archive, $"xl/worksheets/sheet{i + 1}.xml", Worksheet(sheets[i]));
        }
        return buffer.ToArray();
    }

    private static List<object?[]> Overview(AttendanceReportResponse report)
    {
        var o = report.TongQuan;
        return
        [
            ["Học kỳ", $"{report.TenHocKy} ({report.MaHocKy})"],
            ["Số sinh viên tham gia học", o.SoSinhVien],
            ["Số lớp học phần", o.SoLopHocPhan],
            ["Số môn học", o.SoMonHoc],
            ["Số buổi đã diễn ra", o.SoBuoiDaDienRa],
            ["Tổng số buổi theo lịch", o.TongSoBuoi],
            ["Lượt có mặt", o.CoMat],
            ["Lượt đi trễ", o.DiTre],
            ["Lượt vắng có phép", o.CoPhep],
            ["Lượt vắng", o.Vang],
            ["Lượt chưa điểm danh", o.ChuaDiemDanh],
            ["Tỷ lệ chuyên cần", o.TyLeChuyenCan]
        ];
    }

    private static void Add(ZipArchive archive, string path, string content)
    {
        using var stream = archive.CreateEntry(path, CompressionLevel.Optimal).Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string ContentTypes(int sheetCount)
    {
        var builder = new StringBuilder("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
            """);
        for (var i = 1; i <= sheetCount; i++)
            builder.Append($"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""");
        return builder.Append("</Types>").ToString();
    }

    private static string Workbook(IReadOnlyList<Sheet> sheets)
    {
        var builder = new StringBuilder("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>
            """);
        for (var i = 0; i < sheets.Count; i++)
            builder.Append($"""<sheet name="{Escape(sheets[i].Name)}" sheetId="{i + 1}" r:id="rId{i + 1}"/>""");
        return builder.Append("</sheets></workbook>").ToString();
    }

    private static string WorkbookRels(int sheetCount)
    {
        var builder = new StringBuilder("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
            """);
        for (var i = 1; i <= sheetCount; i++)
            builder.Append($"""<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml"/>""");
        builder.Append($"""<Relationship Id="rId{sheetCount + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>""");
        return builder.Append("</Relationships>").ToString();
    }

    // Hai định dạng: 1 là tiêu đề in đậm, 2 là phần trăm cho cột tỷ lệ.
    private static string Styles() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="3"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/><xf numFmtId="9" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/></cellXfs></styleSheet>
        """;

    private static string Worksheet(Sheet sheet)
    {
        var builder = new StringBuilder("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>
            """);
        builder.Append("<row r=\"1\">");
        for (var c = 0; c < sheet.Headers.Count; c++)
            builder.Append($"""<c r="{Reference(c, 1)}" s="1" t="inlineStr"><is><t xml:space="preserve">{Escape(sheet.Headers[c])}</t></is></c>""");
        builder.Append("</row>");

        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var row = sheet.Rows[r];
            builder.Append($"<row r=\"{r + 2}\">");
            for (var c = 0; c < row.Length; c++)
            {
                var reference = Reference(c, r + 2);
                switch (row[c])
                {
                    case null:
                        break;
                    // Tỷ lệ là số thực duy nhất trong báo cáo nên luôn hiển thị dạng phần trăm.
                    case double value:
                        builder.Append($"""<c r="{reference}" s="2"><v>{value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}</v></c>""");
                        break;
                    case int value:
                        builder.Append($"""<c r="{reference}"><v>{value}</v></c>""");
                        break;
                    default:
                        builder.Append($"""<c r="{reference}" t="inlineStr"><is><t xml:space="preserve">{Escape(row[c]!.ToString()!)}</t></is></c>""");
                        break;
                }
            }
            builder.Append("</row>");
        }
        return builder.Append("</sheetData></worksheet>").ToString();
    }

    private static string Reference(int columnIndex, int rowNumber)
    {
        var name = string.Empty;
        for (var i = columnIndex; ; i = i / 26 - 1)
        {
            name = (char)('A' + i % 26) + name;
            if (i < 26) break;
        }
        return name + rowNumber;
    }

    private static string Escape(string value)
    {
        var cleaned = new StringBuilder(value.Length);
        foreach (var character in value)
            if (XmlConvert.IsXmlChar(character)) cleaned.Append(character);
        return new System.Xml.Linq.XText(cleaned.ToString()).ToString();
    }
}
