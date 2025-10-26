using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Products.Dtos;
using MediatR;

namespace Khaikhong.Application.Features.Products.Queries.GetAllProducts;

public sealed record GetAllProductsQuery : IRequest<ApiResponse<List<ProductResponseDto>>>;
