using System.Text.Json.Serialization;

namespace FourMProductSyncPlugin;

[JsonSerializable(typeof(List<FixAssetDepartmentModel>))]
[JsonSerializable(typeof(FixAssetDepartmentModel))]
[JsonSerializable(typeof(ProductModel))]
[JsonSerializable(typeof(List<ProductModel>))]
[JsonSerializable(typeof(StationGroupModel))]
[JsonSerializable(typeof(List<StationGroupModel>))]
[JsonSerializable(typeof(StationModel))]
[JsonSerializable(typeof(List<StationModel>))]
[JsonSerializable(typeof(StationPlanModel))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class JsonContextSerial : JsonSerializerContext
{
    
}