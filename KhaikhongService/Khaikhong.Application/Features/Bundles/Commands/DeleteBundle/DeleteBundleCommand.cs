using System;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Application.Features.Bundles.Queries;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Bundles.Commands.DeleteBundle;

public sealed record DeleteBundleCommand(Guid BundleId) : IRequest<ApiResponse<BundleResponseDto>>;

public sealed class DeleteBundleCommandHandler(
    IBundleRepository bundleRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<DeleteBundleCommandHandler> logger) : IRequestHandler<DeleteBundleCommand, ApiResponse<BundleResponseDto>>
{
    private readonly IBundleRepository _bundleRepository = bundleRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<DeleteBundleCommandHandler> _logger = logger;

    public async Task<ApiResponse<BundleResponseDto>> Handle(DeleteBundleCommand request, CancellationToken cancellationToken)
    {
        Bundle? bundle = await _bundleRepository.GetByIdAsync(request.BundleId, cancellationToken);

        if (bundle is null)
        {
            _logger.LogWarning("Bundle {BundleId} not found for deletion", request.BundleId);
            return ApiResponse<BundleResponseDto>.Fail(
                status: 404,
                message: "Bundle not found");
        }

        if (!bundle.IsActive)
        {
            _logger.LogWarning("Bundle {BundleId} already deleted", request.BundleId);
            return ApiResponse<BundleResponseDto>.Fail(
                status: 400,
                message: "Bundle already deleted");
        }

        bundle.Deactivate();

        foreach (BundleItem item in bundle.Items)
        {
            item.Deactivate();
        }

        Guid? currentUserId = _currentUserService.UserId;
        if (currentUserId.HasValue)
        {
            bundle.SetUpdatedBy(currentUserId.Value);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _unitOfWork.CompleteAsync();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete bundle {BundleId}", request.BundleId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        BundleResponseDto response = BundleResponseMapper.Map(bundle);

        _logger.LogInformation("Bundle {BundleId} deleted successfully", request.BundleId);

        return ApiResponse<BundleResponseDto>.Success(
            status: 200,
            message: "Bundle deleted successfully",
            data: response);
    }
}
