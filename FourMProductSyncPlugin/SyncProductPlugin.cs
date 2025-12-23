using Amazon.Runtime;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using VaultForce.Application.Common.Interfaces.Plugin;
using VaultForce.Application.MessageBus.Generic.Command;
using VaultForce.Domain.Entities.Productions;
using VaultForce.Domain.Entities.User;
using VaultForce.Shared.Generals;
using VaultForce.Shared.Generals.Results;
using VaultForce.Shared.Helpers;
using ErrorType = VaultForce.Shared.Enums.ErrorType;

namespace FourMProductSyncPlugin;

public class SyncProductPlugin : IParameterPlugin
{
    public async Task<Result<object?>> ExecuteAsync(FieldParams fieldParams, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var commandMediator = serviceProvider.GetRequiredService<ICommandMediator>();
        using var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        var logger = serviceProvider.GetRequiredService<ILogger<SyncProductPlugin>>();

        await SyncProductsAsync(fieldParams, logger, commandMediator, httpClient, cancellationToken);
        var createZone = await SyncZoneAsync(fieldParams, logger, commandMediator, httpClient, cancellationToken);
        if (!createZone.IsSuccess)
            return Result<object?>.Failure(createZone.Message, createZone.ErrorType);
        var createStationResult = await SyncStationsAsync(fieldParams, logger, commandMediator, httpClient, createZone.Value, cancellationToken);
        if(!createStationResult.IsSuccess)
            return Result<object?>.Failure(createStationResult.Message, createStationResult.ErrorType);
        var assetRes = await SyncAssetsAsync(fieldParams, logger, commandMediator, httpClient,  createStationResult.Value, cancellationToken);
        if (!assetRes.IsSuccess)
            return Result<object?>.Failure(assetRes.Message, assetRes.ErrorType);
        await SyncStationGroupsAsync(fieldParams, logger, commandMediator, httpClient, createStationResult.Value, cancellationToken);
        var createDepartment = await SyncDepartmentAsync(fieldParams, logger, commandMediator, httpClient, cancellationToken);
        if (!createDepartment.IsSuccess)
            return Result<object?>.Failure(createDepartment.Message, createDepartment.ErrorType);
        return Result<object?>.Success(null);
    }

    private static async  Task<Result<List<ZoneEntity>>> SyncZoneAsync(FieldParams fieldParams, ILogger<SyncProductPlugin> logger,
        ICommandMediator commandMediator, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var baseUri = fieldParams.TryGet<string>("baseUri");
        var requestEndpoint = fieldParams.TryGet<string>("requestZoneEndpoint");
        if (string.IsNullOrEmpty(baseUri))
        {
            logger.LogError("Base Uri are empty");
            return Result<List<ZoneEntity>>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        if (string.IsNullOrEmpty(requestEndpoint))
        {
            logger.LogError("requestZoneEndpoint is empty");
            return Result<List<ZoneEntity>>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        var request = new FluentRequestBuilder(baseUri, httpClient);
        request.SetApiPatternPath(requestEndpoint).AddJsonConverts([..JsonContextSerial.Default.Options.Converters]);
        request.OnException(e =>
        {
            logger.LogError(e, "Error occurred while fetching zone from {RequestEndpoint}", requestEndpoint);
            return Task.CompletedTask;
        });
        var getResult = await request.GetAsync<List<ZoneModel>>(cancellationToken);
        if (getResult == null)
            return Result<List<ZoneEntity>>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        List<ZoneEntity> zoneEntities = new List<ZoneEntity>(getResult.Count);
        foreach (var productModel in getResult)
        {
            var productEntity = new ZoneEntity()
            {
                Code = productModel.ZoneCode,
                Name = productModel.ZoneName,
                Id = ObjectId.Parse(productModel.ObjectId)
            };
            zoneEntities.Add(productEntity);
            CreateEntityCommand<ZoneEntity> createCommand = new CreateEntityCommand<ZoneEntity>(productEntity);
            var re = await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
            if (!re.IsSuccess)
            {
                logger.LogError($"Create product {productModel.ZoneCode} failed: {re.Message}");
            }
            else
            {
                UpdateEntityCommand<ZoneEntity> updateCommand = new UpdateEntityCommand<ZoneEntity>(productEntity);
                await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
            }
        }
        return Result<List<ZoneEntity>>.Success(zoneEntities);
    }
    private static async Task<Result<object?>> SyncProductsAsync(FieldParams fieldParams, ILogger<SyncProductPlugin> logger,
        ICommandMediator commandMediator, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var baseUri = fieldParams.TryGet<string>("baseUri");
        var requestEndpoint = fieldParams.TryGet<string>("requestProductEndpoint");
        if (string.IsNullOrEmpty(baseUri))
        {
            logger.LogError("Base Uri are empty");
            return Result<object?>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        if (string.IsNullOrEmpty(requestEndpoint))
        {
            logger.LogError("requestEndpoint is empty");
            return Result<object?>.Failure("requestEndpoint is empty", ErrorType.InvalidArgument);
        }

        var request = new FluentRequestBuilder(baseUri, httpClient);
        request.SetApiPatternPath(requestEndpoint).AddJsonConverts([..JsonContextSerial.Default.Options.Converters]);
        request.OnException(e =>
        {
            logger.LogError(e, "Error occurred while fetching products from {RequestEndpoint}", requestEndpoint);
            return Task.CompletedTask;
        });
        var getResult = await request.GetAsync<List<ProductModel>>(cancellationToken);
        if (getResult == null)
            return Result<object?>.Failure("Failed to fetch products", ErrorType.ApiError);

        foreach (var productModel in getResult)
        {
            var productEntity = new ProductEntity()
            {
                Code = productModel.ProductCode,
                Name = productModel.ProductName,
                Id = ObjectId.Parse(productModel.ObjectId)
            };
            CreateEntityCommand<ProductEntity> createCommand = new CreateEntityCommand<ProductEntity>(productEntity);
            var re = await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
            if (!re.IsSuccess)
            {
                logger.LogError($"Create product {productModel.ProductCode} failed: {re.Message}");
            }
            else
            {
                UpdateEntityCommand<ProductEntity> updateCommand = new UpdateEntityCommand<ProductEntity>(productEntity);
                await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
            }
        }
        return Result<object?>.Success(null);
    }

    private static async Task<Result<List<StationEntity>>> SyncStationsAsync(FieldParams fieldParams, ILogger<SyncProductPlugin> logger,
        ICommandMediator commandMediator, HttpClient httpClient, List<ZoneEntity> zoneEntities, CancellationToken cancellationToken)
    {
        var baseUri = fieldParams.TryGet<string>("baseUri");
        var requestEndpoint = fieldParams.TryGet<string>("requestStationEndpoint");
        if (string.IsNullOrEmpty(baseUri))
        {
            logger.LogError("Base Uri are empty");
            return Result<List<StationEntity>>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        if (string.IsNullOrEmpty(requestEndpoint))
        {
            logger.LogError("requestEndpoint is empty");
            return Result<List<StationEntity>>.Failure("requestEndpoint is empty", ErrorType.InvalidArgument);
        }

        var request = new FluentRequestBuilder(baseUri, httpClient);
        request.SetApiPatternPath(requestEndpoint).AddJsonConverts([..JsonContextSerial.Default.Options.Converters]);
        request.OnException(e =>
        {
            logger.LogError(e, "Error occurred while fetching products from {RequestEndpoint}", requestEndpoint);
            return Task.CompletedTask;
        });
        var getResult = await request.GetAsync<List<StationModel>>(cancellationToken);
        if (getResult == null)
            return Result<List<StationEntity>>.Failure("Failed to fetch products", ErrorType.ApiError);
        var deleteZoneMappingRes = await commandMediator.SendAsync(
            new DeleteManyEntityCommand<ZoneStationMapping>(_ => true),
            cancellationToken: cancellationToken);

        if (!deleteZoneMappingRes.IsSuccess)
        {
            logger.LogError("Failed to delete existing ZoneStationMapping: {Message}",
                deleteZoneMappingRes.Message);
        }
        List<StationEntity> stationEntities = new List<StationEntity>(getResult.Count);
        foreach (var stationModel in getResult)
        {
            var stationEntity = new StationEntity()
            {
                Code = stationModel.StationCode,
                Name = stationModel.StationName,
                Id = ObjectId.Parse(stationModel.ObjectId)
            };
            if (!string.IsNullOrWhiteSpace(stationModel.ZoneCode))
            {
                var matchedZone = zoneEntities
                    .FirstOrDefault(z =>
                        string.Equals(z.Code, stationModel.ZoneCode,
                            StringComparison.OrdinalIgnoreCase));

                if (matchedZone != null)
                {
                    stationEntity.ZoneId = matchedZone.Id;
                }
                else
                {
                    logger.LogWarning("No Zone found for ZoneCode {ZoneCode} (Station {StationCode})",
                        stationModel.ZoneCode, stationModel.StationCode);
                }
            }
            CreateEntityCommand<StationEntity> createCommand = new CreateEntityCommand<StationEntity>(stationEntity);
            var re = await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
            if (!re.IsSuccess)
            {
                logger.LogError($"Create station group {stationEntity.Code} failed: {re.Message}");
                UpdateEntityCommand<StationEntity> updateCommand = new UpdateEntityCommand<StationEntity>(stationEntity);
                var updateResult = await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
                if (!updateResult.IsSuccess)                {
                    logger.LogError($"Update station group {stationEntity.Code} failed: {updateResult.Message}");
                }            
            }
            stationEntities.Add(stationEntity);
            if (stationEntity.ZoneId != ObjectId.Empty)
            {
                var mapping = new ZoneStationMapping
                {
                    ZoneId = stationEntity.ZoneId,
                    StationId = stationEntity.Id
                };

                var mappingCreateCommand = new CreateEntityCommand<ZoneStationMapping>(mapping);
                var mappingResult = await commandMediator.SendAsync(mappingCreateCommand, cancellationToken: cancellationToken);
                if (!mappingResult.IsSuccess)
                {
                    logger.LogError(
                        "Create mapping for ZoneId {ZoneId} and Station {StationCode} failed: {Message}",
                        mapping.ZoneId, stationEntity.Code, mappingResult.Message);
                }
            }
            
        }
        return Result<List<StationEntity>>.Success(stationEntities);
    }

    private static async Task<Result<List<AssetEntity>>> SyncAssetsAsync(FieldParams fieldParams,
        ILogger<SyncProductPlugin> logger,
        ICommandMediator commandMediator, HttpClient httpClient, List<StationEntity> stationEntities,
        CancellationToken cancellationToken)
    {
         var baseUri = fieldParams.TryGet<string>("baseUri");
        var requestEndpoint = fieldParams.TryGet<string>("requestAssetEndpoint");
        if (string.IsNullOrEmpty(baseUri))
        {
            logger.LogError("Base Uri are empty");
            return Result<List<AssetEntity>>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        if (string.IsNullOrEmpty(requestEndpoint))
        {
            logger.LogError("requestAssetEndpoint is empty");
            return Result<List<AssetEntity>>.Failure("requestAssetEndpoint is empty", ErrorType.InvalidArgument);
        }

        var request = new FluentRequestBuilder(baseUri, httpClient);
        request.SetApiPatternPath(requestEndpoint).AddJsonConverts([..JsonContextSerial.Default.Options.Converters]);
        request.OnException(e =>
        {
            logger.LogError(e, "Error occurred while fetching products from {RequestEndpoint}", requestEndpoint);
            return Task.CompletedTask;
        });
        var url = $"{baseUri!.TrimEnd('/')}/{requestEndpoint!.TrimStart('/')}";

        HttpResponseMessage res;
        string body;

        try
        {
            res = await httpClient.GetAsync(url, cancellationToken);
            body = await res.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTTP error when calling asset endpoint: {Url}", url);
            return Result<List<AssetEntity>>.Failure($"Asset endpoint HTTP exception: {ex.Message}", ErrorType.ApiError);
        }

        if (!res.IsSuccessStatusCode)
        {
            logger.LogError("Asset endpoint failed. Status={StatusCode}. Url={Url}. Body={Body}",
                (int)res.StatusCode, url, body);
            return Result<List<AssetEntity>>.Failure(
                $"Asset endpoint HTTP {(int)res.StatusCode} {res.ReasonPhrase}",
                ErrorType.ApiError);
        }

        List<AssetModel>? getResult;
        try
        {
            var opt = new System.Text.Json.JsonSerializerOptions(JsonContextSerial.Default.Options)
            {
                PropertyNameCaseInsensitive = true
            };

            getResult = System.Text.Json.JsonSerializer.Deserialize<List<AssetModel>>(body, opt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Asset endpoint JSON deserialize failed. Url={Url}. Body={Body}", url, body);
            return Result<List<AssetEntity>>.Failure($"Asset JSON deserialize failed: {ex.Message}", ErrorType.ApiError);
        }

        if (getResult == null)
        {
            logger.LogError("Asset endpoint returned null after deserialize. Url={Url}. Body={Body}", url, body);
            return Result<List<AssetEntity>>.Failure("Asset response is null/invalid", ErrorType.ApiError);
        }

        
        var deleteAssetMappingRes = await commandMediator.SendAsync(
            new DeleteManyEntityCommand<AssetStationMapping>(_ => true),
            cancellationToken: cancellationToken);

        if (!deleteAssetMappingRes.IsSuccess)
        {
            logger.LogError("Failed to delete existing ZoneStationMapping: {Message}",
                deleteAssetMappingRes.Message);
        }
        List<AssetEntity> assetList = new List<AssetEntity>(getResult.Count);
        foreach (var assetModel in getResult)
        {
            var assetEntity = new AssetEntity()
            {
                AssetCode = assetModel.AssetCode,
                AssetName = assetModel.AssetName,
                Id = ObjectId.Parse(assetModel.ObjectId)
            };
            if (!string.IsNullOrWhiteSpace(assetModel.CurrentStation))
            {
                var matchedStation = stationEntities
                    .FirstOrDefault(z =>
                        string.Equals(z.Code, assetModel.CurrentStation,
                            StringComparison.OrdinalIgnoreCase));

                if (matchedStation != null)
                {
                    assetEntity.CurrentStation = matchedStation.Id;
                }
                else
                {
                    logger.LogWarning("No Zone found for ZoneCode {ZoneCode} (Station {StationCode})",
                        assetModel.CurrentStation, assetModel.AssetCode);
                }
            }
            CreateEntityCommand<AssetEntity> createCommand = new CreateEntityCommand<AssetEntity>(assetEntity);
            var re = await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
            if (!re.IsSuccess)
            {
                logger.LogError($"Create station group {assetEntity.AssetCode} failed: {re.Message}");
                UpdateEntityCommand<AssetEntity> updateCommand = new UpdateEntityCommand<AssetEntity>(assetEntity);
                var updateResult = await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
                if (!updateResult.IsSuccess)                {
                    logger.LogError($"Update station group {assetEntity.AssetCode} failed: {updateResult.Message}");
                }            
            }
            assetList.Add(assetEntity);
            if (assetEntity.CurrentStation != ObjectId.Empty)
            {
                var mapping = new AssetStationMapping
                {
                    AssetId = assetEntity.Id,
                    StationId = assetEntity.CurrentStation
                };

                var mappingCreateCommand = new CreateEntityCommand<AssetStationMapping>(mapping);
                var mappingResult = await commandMediator.SendAsync(mappingCreateCommand, cancellationToken: cancellationToken);
                if (!mappingResult.IsSuccess)
                {
                    logger.LogError(
                        "Create mapping for ZoneId {ZoneId} and Station {StationCode} failed: {Message}",
                        mapping.StationId, assetEntity.AssetCode, mappingResult.Message);
                }
            }
            
        }
        return Result<List<AssetEntity>>.Success(assetList);
    }

    private static async Task<Result<List<StationGroupEntity>>> SyncStationGroupsAsync(FieldParams fieldParams, ILogger<SyncProductPlugin> logger,
        ICommandMediator commandMediator, HttpClient httpClient, List<StationEntity> stationEntities, CancellationToken cancellationToken)
    {
        var baseUri = fieldParams.TryGet<string>("baseUri");
        var requestEndpoint = fieldParams.TryGet<string>("requestStationGroupEndpoint");
        if (string.IsNullOrEmpty(baseUri))
        {
            logger.LogError("Base Uri are empty");
            return Result<List<StationGroupEntity>>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        if (string.IsNullOrEmpty(requestEndpoint))
        {
            logger.LogError("requestEndpoint is empty");
            return Result<List<StationGroupEntity>>.Failure("requestEndpoint is empty", ErrorType.InvalidArgument);
        }

        var request = new FluentRequestBuilder(baseUri, httpClient);
        request.SetApiPatternPath(requestEndpoint).AddJsonConverts([..JsonContextSerial.Default.Options.Converters]);
        request.OnException(e =>
        {
            logger.LogError(e, "Error occurred while fetching products from {RequestEndpoint}", requestEndpoint);
            return Task.CompletedTask;
        });
        var getResult = await request.GetAsync<List<StationGroupModel>>(cancellationToken);
        if (getResult == null)
            return Result<List<StationGroupEntity>>.Failure("Failed to fetch products", ErrorType.ApiError);

        var deleteRes = await commandMediator.SendAsync(new DeleteManyEntityCommand<StationGroupStationMapping>(x => true), cancellationToken: cancellationToken);
        if (!deleteRes.IsSuccess)
        {
            logger.LogError("Failed to delete existing StationGroupStationMapping: {Message}", deleteRes.Message);
        }
        
        List<StationGroupEntity> stationGroupEntities = new List<StationGroupEntity>(getResult.Count);
        foreach (var stationGroupModel in getResult)
        {
            var stationGroupEntity = new StationGroupEntity()
            {
                Code = stationGroupModel.StationGroupCode,
                Name = stationGroupModel.GroupName,
                Id = ObjectId.Parse(stationGroupModel.ObjectId)
            };
            stationGroupEntities.Add(stationGroupEntity);
            CreateEntityCommand<StationGroupEntity> createCommand = new CreateEntityCommand<StationGroupEntity>(stationGroupEntity);
            var createResult = await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
            if (!createResult.IsSuccess)
            {
                logger.LogError($"Create station group {stationGroupEntity.Code} failed: {createResult.Message}");
                UpdateEntityCommand<StationGroupEntity> updateCommand = new UpdateEntityCommand<StationGroupEntity>(stationGroupEntity);
                var updateResult = await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
                if (!updateResult.IsSuccess)                {
                    logger.LogError($"Update station group {stationGroupEntity.Code} failed: {updateResult.Message}");
                }
            }
            
            var stationGroupStations = stationGroupModel.StationList
                .Select(stationCode => stationEntities.FirstOrDefault(se => se.Code == stationCode))
                .Where(se => se != null)
                .Select(se => new StationGroupStationMapping()
                {
                    StationGroupId = stationGroupEntity.Id,
                    StationId = se!.Id
                }).ToList();
            foreach (var mapping in stationGroupStations)
            {
                CreateEntityCommand<StationGroupStationMapping> mappingCreateCommand = new CreateEntityCommand<StationGroupStationMapping>(mapping);
                var re = await commandMediator.SendAsync(mappingCreateCommand, cancellationToken: cancellationToken);
                if (!re.IsSuccess)
                {
                    logger.LogError($"Create mapping for StationGroup {stationGroupEntity.Code} and Station {mapping.StationId} failed: {re.Message}");
                }
            }

            var stationInGroups = stationEntities.Where(x => stationGroupModel.StationList.Contains(x.Code)).ToList();
            logger.LogInformation($"Found {stationGroupEntities.Count} StationGroups");
            foreach (var station in stationInGroups)
            {
                station.StationGroup = stationGroupEntity.Id;
                var updateCommand = new UpdateEntityCommand<StationEntity>(station);
                var re = await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
                if (!re.IsSuccess)
                {
                    logger.LogError($"Update station {station.Code} failed: {re.Message}");
                }
            }
        }
        return Result<List<StationGroupEntity>>.Success(stationGroupEntities);
    }
    
    private static async Task<Result<List<DepartmentEntity>>> SyncDepartmentAsync(FieldParams fieldParams,
        ILogger<SyncProductPlugin> logger,
        ICommandMediator commandMediator, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var baseUri = fieldParams.TryGet<string>("baseUri");
        var requestEndpoint = fieldParams.TryGet<string>("requestDepartmentEndpoint");
        if (string.IsNullOrEmpty(baseUri))
        {
            logger.LogError("Base Uri are empty");
            return Result<List<DepartmentEntity>>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        if (string.IsNullOrEmpty(requestEndpoint))
        {
            logger.LogError("requestEndpoint is empty");
            return Result<List<DepartmentEntity>>.Failure("requestEndpoint is empty", ErrorType.InvalidArgument);
        }

        var request = new FluentRequestBuilder(baseUri, httpClient);
        request.SetApiPatternPath(requestEndpoint).AddJsonConverts([..JsonContextSerial.Default.Options.Converters]);
        request.OnException(e =>
        {
            logger.LogError(e, "Error occurred while fetching products from {RequestEndpoint}", requestEndpoint);
            return Task.CompletedTask;
        });
        var getResult = await request.GetAsync<List<FixAssetDepartmentModel>>(cancellationToken);
        if (getResult == null)
            return Result<List<DepartmentEntity>>.Failure("Failed to fetch products", ErrorType.ApiError);
        List<DepartmentEntity> departmentEntities = new List<DepartmentEntity>(getResult.Count);
        foreach (var departmentModel in getResult)
        {
            var departmentEntity = new DepartmentEntity()
            {
                Code = departmentModel.DepartmentCode,
                Name = departmentModel.DepartmentName,
                Id = ObjectId.Parse(departmentModel.ObjectId)
            };
            departmentEntities.Add(departmentEntity);
            CreateEntityCommand<DepartmentEntity> createCommand =
                new CreateEntityCommand<DepartmentEntity>(departmentEntity);
            var re = await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
            if (!re.IsSuccess)
            {
                logger.LogError($"Create department {departmentEntity.Code} failed: {re.Message}");
            }
            else
            {
                UpdateEntityCommand<DepartmentEntity> updateCommand = new UpdateEntityCommand<DepartmentEntity>(departmentEntity);
                await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
            }
            
        }
        return Result<List<DepartmentEntity>>.Success(departmentEntities);
    }
    
}