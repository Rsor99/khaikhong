using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Bundles.Commands.DeleteBundle;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Khaikhong.Tests.Features.Bundles.Commands.DeleteBundle;

public sealed class DeleteBundleCommandHandlerTests
{
    private readonly Mock<IBundleRepository> _bundleRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<DeleteBundleCommandHandler>> _logger = new();

    public DeleteBundleCommandHandlerTests()
    {
        Mock<IUnitOfWorkTransaction> transactionMock = new();
        transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _unitOfWork.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBundleMissing()
    {
        Guid bundleId = Guid.CreateVersion7();

        _bundleRepository
            .Setup(repo => repo.GetByIdAsync(bundleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bundle?)null);

        DeleteBundleCommandHandler handler = BuildHandler();

        ApiResponse<BundleResponseDto> response = await handler.Handle(new DeleteBundleCommand(bundleId), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenBundleAlreadyInactive()
    {
        Guid bundleId = Guid.CreateVersion7();
        Bundle bundle = Bundle.Create("Eco Starter Kit", 1290m, "Bundle");
        Deactivate(bundle);

        _bundleRepository
            .Setup(repo => repo.GetByIdAsync(bundleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundle);

        DeleteBundleCommandHandler handler = BuildHandler();

        ApiResponse<BundleResponseDto> response = await handler.Handle(new DeleteBundleCommand(bundleId), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteBundle_WhenFound()
    {
        Guid bundleId = Guid.CreateVersion7();
        Guid productId = Guid.Parse("019a2104-81df-7364-8fa9-61702d30c11a");

        Bundle bundle = Bundle.Create("Eco Starter Kit", 1290m, "Bundle");
        SetId(bundle, bundleId);

        BundleItem item = BundleItem.Create(bundleId, 2, productId);
        SetNavigation(item, bundle, null, null);
        bundle.AddItems(new[] { item });

        _bundleRepository
            .Setup(repo => repo.GetByIdAsync(bundleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundle);

        _bundleRepository
            .Setup(repo => repo.GetDetailedByIdAsync(bundleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundle);

        Guid currentUserId = Guid.Parse("019a3aaa-dead-beef-b429-d4def7147777");
        _currentUserService
            .Setup(service => service.UserId)
            .Returns(currentUserId);

        DeleteBundleCommandHandler handler = BuildHandler();

        ApiResponse<BundleResponseDto> response = await handler.Handle(new DeleteBundleCommand(bundleId), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.False(bundle.IsActive);
        Assert.Equal(currentUserId, bundle.UpdatedBy);
        Assert.All(bundle.Items, item => Assert.False(item.IsActive));
    }

    private DeleteBundleCommandHandler BuildHandler() => new(
        _bundleRepository.Object,
        _unitOfWork.Object,
        _currentUserService.Object,
        _logger.Object);

    private static void SetId(object entity, Guid id)
    {
        FieldInfo? idField = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        idField?.SetValue(entity, id);
    }

    private static void SetNavigation(BundleItem item, Bundle bundle, Product? product, Variant? variant)
    {
        FieldInfo? bundleField = typeof(BundleItem).GetField("<Bundle>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        bundleField?.SetValue(item, bundle);

        FieldInfo? productField = typeof(BundleItem).GetField("<Product>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        productField?.SetValue(item, product);

        FieldInfo? variantField = typeof(BundleItem).GetField("<Variant>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        variantField?.SetValue(item, variant);
    }

    private static void Deactivate(Bundle bundle)
    {
        bundle.Deactivate();
        foreach (BundleItem item in bundle.Items)
        {
            item.Deactivate();
        }
    }
}
