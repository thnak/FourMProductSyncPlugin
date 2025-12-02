namespace FourMProductSyncPlugin;

public class FixAssetDepartmentModel
{
    public string ObjectId { get; set; } = string.Empty;

    public string DepartmentCode { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public string Site { get; set; } = string.Empty;

    public string CreateUser { get; set; } = string.Empty;

    public int CreateTime { get; set; }
    public string Alias { get; set; } = string.Empty; // một số hệ thống cũ đang sử dụng Alias để lưu mã phòng ban
}