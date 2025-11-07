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

        var baseUri = fieldParams.TryGet<string>("baseUri");
        var requestEndpoint = fieldParams.TryGet<string>("requestEndpoint");
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
        request.SetApiPatternPath(requestEndpoint);
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
            CreateEntityCommand<ProductEntity> createCommand = new CreateEntityCommand<ProductEntity>(
                new ProductEntity()
                {
                    Code = productModel.ProductCode,
                    Name = productModel.ProductName,
                    Id = ObjectId.Parse(productModel.ObjectId)
                });
            await commandMediator.SendAsync(createCommand, cancellationToken: cancellationToken);
        }

        return Result<object?>.Success(null);
    }
}