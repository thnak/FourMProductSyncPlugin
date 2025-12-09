using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace FourMProductSyncPlugin;

public class ZoneModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ObjectId { get; set; }

    //[PrimaryKey]
    [JsonProperty] public string ZoneCode { get; set; }

    [JsonProperty] public string ZoneName { get; set; }

    [JsonProperty] public string Description { get; set; }

    [JsonProperty] public string Icon { get; set; }

    [JsonProperty] public int LastUpdate { get; set; }

    [JsonProperty] public bool IsEnable { get; set; }

    [JsonProperty] public ZoneType Type { get; set; } = ZoneType.WorkStation;

    [JsonProperty] public Dictionary<string, string> Infos { get; set; }
    [JsonProperty] public string ManagerDept { get; set; } = string.Empty;

    public override string ToString()
    {
        return ZoneName;
    }
}