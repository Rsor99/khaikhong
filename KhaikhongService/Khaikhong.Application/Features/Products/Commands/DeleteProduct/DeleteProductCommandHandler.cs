using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IBundleRepository bundleRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteProductCommandHandler> logger) : IRequestHandler<DeleteProductCommand, ApiResponse<object>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IBundleRepository _bundleRepository = bundleRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<DeleteProductCommandHandler> _logger = logger;

    public async Task<ApiResponse<object>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading product {ProductId} for deletion", request.ProductId);

        Product? product = await _productRepository.GetDetailedByIdTrackingAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product {ProductId} not found for deletion", request.ProductId);
            return ApiResponse<object>.Fail(
                status: 404,
                message: "Product not found",
                errors: new[]
                {
                    new { field = "productId", error = "Product does not exist." }
                });
        }

        if (await _bundleRepository.IsProductLinkedAsync(request.ProductId, cancellationToken))
        {
            return ApiResponse<object>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "productId", error = "Product is linked to one or more bundles. Unlink them before deletion." }
                });
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            SoftDelete(product);

            await _unitOfWork.CompleteAsync();
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} soft deleted successfully", request.ProductId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error soft deleting product {ProductId}", request.ProductId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return ApiResponse<object>.Success(
            status: 200,
            message: "Product deleted successfully",
            data: new { productId = request.ProductId });
    }

    private static void SoftDelete(Product product)
    {
        product.Deactivate();

        foreach (VariantOption option in product.Options)
        {
            option.Deactivate();
            foreach (VariantOptionValue value in option.Values)
            {
                value.Deactivate();
            }
        }

        foreach (Variant variant in product.Variants)
        {
            variant.Deactivate();

            foreach (ProductVariantCombination combination in variant.Combinations)
            {
                combination.Deactivate();
            }
        }
    }
}
