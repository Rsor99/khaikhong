using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Bundles.Dtos;
using MediatR;

namespace Khaikhong.Application.Features.Bundles.Commands.CreateBundle;

public sealed record CreateBundleCommand(CreateBundleRequestDto Request) : IRequest<ApiResponse<CreateBundleResponseDto>>;
