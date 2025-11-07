using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using VaultForce.Shared.Enums;

namespace FourMProductSyncPlugin;

public class ProductModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ObjectId { get; set; }

    [JsonProperty] public string ProductCode { get; set; }
    [JsonProperty] public string ProductName { get; set; }
    [JsonProperty] public string AliasCode { get; set; }
    [JsonProperty] public string AliasName { get; set; }
    [JsonProperty] public string RoutingCode { get; set; }
    [JsonProperty] public ServerUnit Unit { get; set; }
    [JsonProperty] public double WeightUnit { get; set; }
    [JsonProperty] public double Coefficient { get; set; }
    [JsonProperty] public int UpdateTime { get; set; }
    [JsonProperty] public string UpdateUser { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [JsonProperty]
    public string StationGroupId { get; set; } = string.Empty;
    [JsonProperty] public ProductType ProductType { get; set; }

    [JsonProperty] public string ProductGroup { get; set; } = string.Empty;
    [JsonProperty] public Dictionary<string, string> Infors { get; set; } = [];

    [JsonProperty] public string BoxQty { get; set; } = string.Empty;
    public ObjectId ProductLineId { get; set; }
    public ObjectId ThermalPatternId { get; set; }
    [JsonProperty] public string Customer { get; set; } = string.Empty;
    [JsonProperty] public string CustomerModelCode { get; set; } = string.Empty;

    /// <summary>
    /// Pattern code for group Product parameter
    /// </summary>
    [JsonProperty]
    public string PatternCode { get; set; } = string.Empty;

    public override string ToString()
    {
        return ProductCode;
    }
}