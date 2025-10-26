using System;
using Khaikhong.Application.Common.Models;
using MediatR;

namespace Khaikhong.Application.Features.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest<ApiResponse<object>>;
