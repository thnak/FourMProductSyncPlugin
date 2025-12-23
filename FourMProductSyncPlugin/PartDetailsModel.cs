using Newtonsoft.Json;

namespace FourMProductSyncPlugin;
[JsonObject(MemberSerialization.OptIn)]
public class PartDetailsModel
{
    int _IotRunTime = 0;
    double _LifeRunHours = 0;
    [JsonProperty] public string PartCode { get; set; } = "";
    [JsonProperty] public double Quantity { get; set; }
    /// <summary>
    /// Tuổi thọ theo thông số nhà sản xuất Đơn vị là giờ
    /// </summary>
    [JsonProperty] public int MakerLifespan { get; set; }

    /// <summary>
    /// Đơn vị của tuổi thọ theo thông số nhà sản xuất ví dụ: Giờ, Ngày, Tháng, Năm
    /// </summary>
    [JsonProperty]
    public string MakerLifespanUnit { get; set; } = string.Empty;  
    /// <summary>
    /// Tuổi thọ cấu hình (Seting ), tính theo đơn vị Tháng. 
    /// </summary>
    [JsonProperty] public int Lifespan { get; set; }
    /// <summary>
    /// Số giờ đã chạy tính theo IoT
    /// </summary>
    [JsonProperty] public double LifeRunHours
    {
        get
        {
            return _LifeRunHours;
        }
        set
        {
            _IotRunTime = (int)(value * 3600);
            _LifeRunHours = value;
        }
    }
    [JsonProperty] public int ReplacementDate { get; set; }
    [JsonProperty] public PriorityPartType TypePriority { get; set; }
    [JsonProperty] public int CurrentInventoryQuantity { get; set; }
    [JsonProperty] public int IotRunTime {
        get
        {
            return _IotRunTime;
        }
        set
        {
            _LifeRunHours = (double)value/3600;
            _IotRunTime = value;    
        }
    } // seconds
    [JsonProperty] public int IotCounters{ get; set; } // times
    [JsonProperty] public DateTime LastIotUpdateTime { get; set; }
}