using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using VaultForce.Shared.Converters;

namespace FourMProductSyncPlugin;

[JsonObject(MemberSerialization.OptIn)]
public class StationModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ObjectId { get; set; }

    [JsonProperty]
    public string StationCode { get; set; } = "";

    [JsonProperty]
    public string StationName { get; set; } = "";

    [JsonProperty]
    public string ZoneCode { get; set; } = "";

    [JsonProperty]
    public string CurrentProduct { get; set; } = "";

    [JsonProperty]
    public string CurrentEmployee { get; set; } = "";

    [JsonProperty]
    public string UserCreate { get; set; } = "";

    [JsonProperty]
    public int InsertTime { get; set; }

    [JsonProperty]
    public bool IsStandalone { get; set; } = true;

    [JsonProperty]
    public Dictionary<string, string> Infors { get; set; } = new(); // Reference to StationPropertiesModel

    [JsonProperty]
    public JobsRunType JobsRunType { get; set; } = JobsRunType.PlanningJobs;

    [JsonProperty]
    [Obsolete("Use StationGroup type instead")]
    public StationType StationType { get; set; }

    [JsonProperty]
    public bool SplitJobsByShift { get; set; } = false;

    [JsonProperty]
    public bool RequireWorkOrder { get; set; } = false;

    [JsonProperty]
    public bool MachineRunQtyIsOutput { get; set; } = false;

    [System.Text.Json.Serialization.JsonConverter(typeof(ObjectIdConverter))]
    public ObjectId MachineModelId { get; set; } = MongoDB.Bson.ObjectId.Empty;

    /// <summary>
    /// If it is automachine, it's mean the Line DON'T HAVE Breath Time. 
    /// </summary>
    [JsonProperty]
    public bool IsAutoStation { get; set; } = false;

    /// <summary>
    /// If station using Kanban
    /// </summary>
    [JsonProperty]
    public bool IsUsingKanban { get; set; } = false;

    /// <summary>
    /// If station using Kanban
    /// </summary>
    [JsonProperty]
    public double KanbanQty { get; set; } = 0;

    [JsonProperty]
    public double StandardCycleTime { get; set; }

    [JsonProperty]
    public string CurrentDowntimeCode { get; set; }

    /// <summary>
    /// Next Station Plan Information
    /// </summary>
    [JsonProperty]
    public StationPlanModel NextPlan { get; set; } = new();

    public override string ToString()
    {
        return StationName;
    }
}