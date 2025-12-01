namespace FourMProductSyncPlugin;

public class StationPlanModel
{
    public StationPlanModel()
    {
        ProductCode = "";
        Quantity = -1;
        StartWorkingTime = -1;
        StandardCT = 50;
        IsNextPlanConfirmed = false;
    }

    public string ProductCode { get; set; }

    /// <summary>
    /// So luong sang pham ke hoach chay
    /// </summary>
    public double Quantity { get; set; }

    public double TotalPlanQuantity { get; set; }

    public double DailyTarget { get; set; }

    public int StartWorkingTime { get; set; }

    public double StandardCT { get; set; }

    public string UpdateUser { get; set; } = string.Empty;

    public int UpdateTime { get; set; }

    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Note đã xác nhận hay chưa?
    /// </summary>
    public bool IsNextPlanConfirmed { get; set; } = false;
}