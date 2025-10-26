using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Products.Dtos;
using MediatR;

namespace Khaikhong.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid ProductId, UpdateProductRequestDto Request) : IRequest<ApiResponse<CreateProductResponseDto>>;
