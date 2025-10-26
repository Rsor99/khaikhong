using System;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Application.Features.Bundles.Queries;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Bundles.Queries.GetBundleById;

public sealed record GetBundleByIdQuery(Guid Id) : IRequest<ApiResponse<BundleResponseDto>>;

public sealed class GetBundleByIdQueryHandler(
    IBundleRepository bundleRepository,
    ILogger<GetBundleByIdQueryHandler> logger) : IRequestHandler<GetBundleByIdQuery, ApiResponse<BundleResponseDto>>
{
    private readonly IBundleRepository _bundleRepository = bundleRepository;
    private readonly ILogger<GetBundleByIdQueryHandler> _logger = logger;

    public async Task<ApiResponse<BundleResponseDto>> Handle(GetBundleByIdQuery request, CancellationToken cancellationToken)
    {
        Bundle? bundle = await _bundleRepository.GetDetailedByIdAsync(request.Id, cancellationToken);

        if (bundle is null)
        {
            _logger.LogWarning("Bundle {BundleId} not found or inactive", request.Id);

            return ApiResponse<BundleResponseDto>.Fail(
                status: 404,
                message: "Bundle not found");
        }

        BundleResponseDto response = BundleResponseMapper.Map(bundle);

        _logger.LogInformation("Bundle {BundleId} retrieved successfully", bundle.Id);

        return ApiResponse<BundleResponseDto>.Success(
            status: 200,
            message: "Bundle retrieved successfully",
            data: response);
    }
}
