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
        await SyncStationGroupsAsync(fieldParams, logger, commandMediator, httpClient, createStationResult.Value, cancellationToken);
        await SyncDepartmentAsync(fieldParams, logger, commandMediator, httpClient, cancellationToken);
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
            logger.LogError("requestEndpoint is empty");
            return Result<List<ZoneEntity>>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        var request = new FluentRequestBuilder(baseUri, httpClient);
        request.SetApiPatternPath(requestEndpoint).AddJsonConverts([..JsonContextSerial.Default.Options.Converters]);
        request.OnException(e =>
        {
            logger.LogError(e, "Error occurred while fetching products from {RequestEndpoint}", requestEndpoint);
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

        List<StationEntity> stationEntities = new List<StationEntity>(getResult.Count);
        foreach (var stationModel in getResult)
        {
            var stationEntity = new StationEntity()
            {
                Code = stationModel.StationCode,
                Name = stationModel.StationName,
                Id = ObjectId.Parse(stationModel.ObjectId)
            };
            stationEntities.Add(stationEntity);
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
            var zoneStationMappings = stationModel.ZoneCode
                .Select(code => zoneEntities.FirstOrDefault(z => z.Code == code.ToString()))
                .Where(z => z != null)
                .Select(z => new ZoneStationMapping
                {
                    ZoneId = z!.Id,               // Id của Zone
                    StationId = stationEntity.Id, // Id của Station
                })
                .ToList();
            foreach (var mapping in zoneStationMappings)
            {
                CreateEntityCommand<ZoneStationMapping> mappingCreateCommand = new CreateEntityCommand<ZoneStationMapping>(mapping);
                var results = await commandMediator.SendAsync(mappingCreateCommand, cancellationToken: cancellationToken);
                if (!results.IsSuccess)
                {
                    logger.LogError($"Create mapping for StationGroup {stationEntity.Code} and Station {mapping.StationId} failed: {re.Message}");
                }
            }
            var stationInGroups = stationEntities.Where(x => x.ZoneId == stationEntity.ZoneId).ToList();
            logger.LogInformation($"Found {stationEntities.Count} StationGroups");
            foreach (var station in stationInGroups)
            {
                station.StationGroup = stationEntity.Id;
                var updateCommand = new UpdateEntityCommand<StationEntity>(station);
                var me = await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
                if (!me.IsSuccess)
                {
                    logger.LogError($"Update station {station.Code} failed: {re.Message}");
                }
            }
            
        }
        return Result<List<StationEntity>>.Success(stationEntities);
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
            UpdateEntityCommand<DepartmentEntity> updateCommand = new UpdateEntityCommand<DepartmentEntity>(departmentEntity);
            await commandMediator.SendAsync(updateCommand, cancellationToken: cancellationToken);
        }
        return Result<List<DepartmentEntity>>.Success(departmentEntities);
    }
}