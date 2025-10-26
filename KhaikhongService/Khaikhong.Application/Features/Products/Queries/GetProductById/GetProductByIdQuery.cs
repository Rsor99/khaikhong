using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Products.Dtos;
using MediatR;

namespace Khaikhong.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ApiResponse<ProductResponseDto>>;
