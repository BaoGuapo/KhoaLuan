using System.Data.Common;

namespace QuanLySinhVien.Api.Data;

public static class DbCommandExtensions
{
    /// <summary>Gắn tham số vào câu lệnh SQL thô để giá trị người dùng nhập không bị ghép thẳng vào chuỗi SQL.</summary>
    public static void AddParameter(this DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
