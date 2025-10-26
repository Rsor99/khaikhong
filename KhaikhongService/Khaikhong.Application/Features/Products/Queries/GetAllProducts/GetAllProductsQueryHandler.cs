using AutoMapper;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Products.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Products.Queries.GetAllProducts;

public sealed class GetAllProductsQueryHandler(
    IProductRepository productRepository,
    IMapper mapper,
    ILogger<GetAllProductsQueryHandler> logger) : IRequestHandler<GetAllProductsQuery, ApiResponse<List<ProductResponseDto>>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<GetAllProductsQueryHandler> _logger = logger;

    public async Task<ApiResponse<List<ProductResponseDto>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Domain.Entities.Product> products = await _productRepository.GetAllDetailedAsync(cancellationToken);

        List<ProductResponseDto> response = _mapper.Map<List<ProductResponseDto>>(products);

        _logger.LogInformation("Retrieved {Count} products", response.Count);

        return ApiResponse<List<ProductResponseDto>>.Success(
            status: 200,
            message: "Products retrieved successfully",
            data: response);
    }
}
