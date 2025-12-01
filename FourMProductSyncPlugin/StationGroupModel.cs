using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace FourMProductSyncPlugin;

[JsonObject(MemberSerialization.OptIn)]
public class StationGroupModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ObjectId { get; set; }

    [JsonProperty] public string StationGroupCode { get; set; } = string.Empty;

    [JsonProperty] public string GroupName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    [JsonProperty] public List<string> StationList { get; set; } = new();

    [JsonProperty]
    public StationType StationType { get; set; }
		
    [JsonProperty] public string Note { get; set; } = string.Empty;

    [JsonProperty] public int Priority { get; set; }
}