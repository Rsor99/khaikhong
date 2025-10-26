using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Application.Features.Bundles.Queries;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Bundles.Queries.GetAllBundles;

public sealed record GetAllBundlesQuery : IRequest<ApiResponse<List<BundleResponseDto>>>;

public sealed class GetAllBundlesQueryHandler(
    IBundleRepository bundleRepository,
    ILogger<GetAllBundlesQueryHandler> logger) : IRequestHandler<GetAllBundlesQuery, ApiResponse<List<BundleResponseDto>>>
{
    private readonly IBundleRepository _bundleRepository = bundleRepository;
    private readonly ILogger<GetAllBundlesQueryHandler> _logger = logger;

    public async Task<ApiResponse<List<BundleResponseDto>>> Handle(GetAllBundlesQuery request, CancellationToken cancellationToken)
    {
        List<Bundle> bundles = await _bundleRepository.GetAllDetailedAsync(cancellationToken);

        List<BundleResponseDto> response = bundles
            .Select(BundleResponseMapper.Map)
            .ToList();

        _logger.LogInformation("Retrieved {BundleCount} active bundles", response.Count);

        return ApiResponse<List<BundleResponseDto>>.Success(
            status: 200,
            message: "Bundles retrieved successfully",
            data: response);
    }
}
