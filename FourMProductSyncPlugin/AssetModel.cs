using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace FourMProductSyncPlugin;

[JsonObject(MemberSerialization.OptIn)]
public class AssetModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ObjectId { get; set; }

    /// <summary>
    /// Mã V, mã máy
    /// </summary>
    [JsonProperty]
    public string AssetCode { get; set; } = string.Empty;

    [JsonProperty]
    public string AccountingCode { get; set; } = string.Empty;

    [JsonProperty]
    public string AssetName { get; set; } = string.Empty;

    [JsonProperty]
    public Dictionary<string, string> Properties { get; set; } = new();

    [JsonProperty]
    public string AssetClassCode { get; set; } = "";

    /// <summary>
    /// Model máy
    /// </summary>
    [JsonProperty]
    public string? EquipmentCode { get; set; }

    /// <summary>
    /// RFID Code
    /// </summary>
    [JsonProperty]
    public string? RfidCode { get; set; }

    [JsonProperty]
    public List<PartDetailsModel> ChildCodes { get; set; } = new();

    [JsonProperty]
    public string? Description { get; set; }

    [JsonProperty]
    public int CreateTime { get; set; }

    /// <summary>
    /// Thời điểm Reset Chu Kỳ tính máy chạy để phục vụ bảo trì. 
    /// Thường sau khi hoàn thành bảo trì thời gian này sẽ tự động reset. 
    /// </summary>
    [JsonProperty]
    public int StartUsingTime { get; set; } // Cho định kỳ maintenance

    /// <summary>
    /// Thời gian sử dụng tính theo giây. 
    /// </summary>
    [JsonProperty]
    public int ActualUsingTime { get; set; } // Cho bảo trì theo thời gian sử dụng thực tế

    [JsonProperty]
    public int ActualSignCounter { get; set; } // Bỏ trì the số lượng sản phẩm chạy

    [JsonProperty]
    public string MaintenanceCycle { get; set; }

    [JsonIgnore]
    [BsonIgnore]
    public double DoubleMaintenanceCycle
    {
        get
        {
            if (double.TryParse(MaintenanceCycle, out var value))
                return value;
            return 0;
        }
    }

    [JsonProperty]
    public int UpdateTime { get; set; }

    [JsonProperty]
    public int UpdateLocationChangeTime { get; set; }

    [JsonProperty]
    public int InsertUser { get; set; }

    [JsonProperty]
    public int UpdateUser { get; set; }

    [JsonProperty]
    public int CurrentZone { get; set; }

    [JsonProperty]
    public string? Proccess { get; set; }

    [JsonProperty]
    public string? CurrentStation { get; set; }

    [JsonProperty]
    public string? LocationCode { get; set; } = "";

    /// <summary>
    /// Sử dụng mã cho Qr-Code hoặc Barcode.
    /// </summary>
    [JsonProperty]
    public string? BarcodeLablel { get; set; }

    [JsonProperty]
    public string? FixAssetNumber { get; set; }

    [JsonProperty]
    public string? Site { get; set; }

    [JsonProperty]
    public FixAssetGroupEnum FixAssetGroup { get; set; } = FixAssetGroupEnum.Other;

    [JsonProperty]
    public string? FixAssetLocation { get; set; }

    [JsonProperty]
    public string? FixAssetDepartment { get; set; }

    [JsonProperty]
    public string? FixAssetPicUser { get; set; }

    [JsonProperty]
    public bool IsMandatoryInventory { get; set; } = true;

    [JsonProperty]
    public InventoryStatusType Status { get; set; } = InventoryStatusType.Open;
        
    public AssetApprovalStatus ApprovalStatus { get; set; } = AssetApprovalStatus.ProposedAcceptance;

    [JsonProperty]
    public string Note { get; set; } = "";

    [JsonProperty]
    public List<string> MongoFileIds { get; set; } = [];
        
    public List<string> EdocCodes { get; set; } = [];
    public string LastMaintenanceWo { get; set; }
    
    public override string ToString()
    {
        return AssetName;
    }
}