using AutoMapper;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Products.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(
    IProductRepository productRepository,
    IMapper mapper,
    ILogger<GetProductByIdQueryHandler> logger) : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductResponseDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<GetProductByIdQueryHandler> _logger = logger;

    public async Task<ApiResponse<ProductResponseDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        Domain.Entities.Product? product = await _productRepository.GetDetailedByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product {ProductId} not found", request.ProductId);

            return ApiResponse<ProductResponseDto>.Fail(
                status: 404,
                message: "Product not found",
                errors: new[]
                {
                    new { field = "productId", error = "Product does not exist." }
                });
        }

        ProductResponseDto response = _mapper.Map<ProductResponseDto>(product);

        _logger.LogInformation("Product {ProductId} retrieved", request.ProductId);

        return ApiResponse<ProductResponseDto>.Success(
            status: 200,
            message: "Product retrieved successfully",
            data: response);
    }
}
