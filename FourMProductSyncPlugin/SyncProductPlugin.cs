using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using VaultForce.Application.Common.Interfaces.Plugin;
using VaultForce.Application.MessageBus.Generic.Command;
using VaultForce.Domain.Entities.Productions;
using VaultForce.Shared.Enums;
using VaultForce.Shared.Generals;
using VaultForce.Shared.Generals.Results;
using VaultForce.Shared.Helpers;

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
        var createStationResult = await SyncStationsAsync(fieldParams, logger, commandMediator, httpClient, cancellationToken);
        if(!createStationResult.IsSuccess)
            return Result<object?>.Failure(createStationResult.Message, createStationResult.ErrorType);
        await SyncStationGroupsAsync(fieldParams, logger, commandMediator, httpClient, createStationResult.Value, cancellationToken);
        
        return Result<object?>.Success(null);
    }

    private static async Task<Result<object?>> SyncProductsAsync(FieldParams fieldParams, ILogger<SyncProductPlugin> logger,
        ICommandMediator commandMediator, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var baseUri = fieldParams.TryGet<string>("baseUri");
        var productRequestEndpoint = fieldParams.TryGet<string>("requestProductEndpoint");
        if (string.IsNullOrEmpty(baseUri))
        {
            logger.LogError("Base Uri are empty");
            return Result<object?>.Failure("baseUri is empty", ErrorType.InvalidArgument);
        }

        if (string.IsNullOrEmpty(productRequestEndpoint))
        {
            logger.LogError("requestEndpoint is empty");
            return Result<object?>.Failure("requestEndpoint is empty", ErrorType.InvalidArgument);
        }

        var request = new FluentRequestBuilder(baseUri, httpClient);
        request.SetApiPatternPath(productRequestEndpoint);
        request.OnException(e =>
        {
            logger.LogError(e, "Error occurred while fetching products from {RequestEndpoint}", productRequestEndpoint);
            return Task.CompletedTask;
        });
        var getResult = await request.GetAsync<List<ProductModel>>(cancellationToken);
        if (getResult == null)
            return Result<object?>.Failure("Failed to fetch products", ErrorType.ApiError);

        foreach (var productModel in getResult)
        {
            CreateEntityCommand<ProductEntity> createCommand = new CreateEntityCommand<ProductEntity>(
                new ProductEntity()
                {
                    Code = productModel.ProductCode,
                    Name = productModel.ProductName,
                    Id = ObjectId.Parse(productModel.ObjectId)
                });
            var re = await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
            if (!re.IsSuccess)
            {
                logger.LogError($"Create product {productModel.ProductCode} failed: {re.Message}");
            }
        }
        return Result<object?>.Success(null);
    }

    private static async Task<Result<List<StationEntity>>> SyncStationsAsync(FieldParams fieldParams, ILogger<SyncProductPlugin> logger,
        ICommandMediator commandMediator, HttpClient httpClient, CancellationToken cancellationToken)
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
        request.SetApiPatternPath(requestEndpoint);
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
                logger.LogError($"Create station {stationEntity.Code} failed: {re.Message}");
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
        request.SetApiPatternPath(requestEndpoint);
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
            await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
            
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
    
    private static async Task MapStationToGroupAsync(List<StationEntity> stationEntities, List<StationGroupModel> stationGroupModels,
        ICommandMediator commandMediator, CancellationToken cancellationToken)
    {
        
    }
}