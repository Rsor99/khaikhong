using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Bundles.Commands.UpdateBundle;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Khaikhong.Tests.Features.Bundles.Commands.UpdateBundle;

public sealed class UpdateBundleCommandHandlerTests
{
    private readonly Mock<IBundleRepository> _bundleRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<UpdateBundleCommandHandler>> _logger = new();

    public UpdateBundleCommandHandlerTests()
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
        UpdateBundleCommand command = new(Guid.CreateVersion7(), BuildRequest());

        _productRepository
            .Setup(repo => repo.AreProductsActiveAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productRepository
            .Setup(repo => repo.GetActiveVariantsForProductsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<Guid>>());

        _bundleRepository
            .Setup(repo => repo.GetDetailedByIdTrackingAsync(command.BundleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bundle?)null);

        UpdateBundleCommandHandler handler = BuildHandler();

        ApiResponse<BundleResponseDto> response = await handler.Handle(command, CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Handle_ShouldUpdateBundle_WhenValid()
    {
        Guid bundleId = Guid.CreateVersion7();
        Guid shirtProductId = Guid.Parse("019a3662-aaaa-4f80-929d-742201aa1111");
        Guid mugProductId = Guid.Parse("019a3662-bbbb-4f80-929d-742201aa2222");
        Guid mugVariantId = Guid.Parse("019a3662-cccc-4f80-929d-742201aa3333");
        Guid bottleProductId = Guid.Parse("019a3662-dddd-4f80-929d-742201aa4444");

        Bundle bundle = Bundle.Create("Starter", 100m, "Original");
        SetId(bundle, bundleId);
        BundleItem existingBaseItem = BundleItem.Create(bundle.Id, 1, shirtProductId);
        BundleItem existingVariantItem = BundleItem.Create(bundle.Id, 1, mugProductId, mugVariantId);
        bundle.AddItems(new[] { existingBaseItem, existingVariantItem });

        Product shirtProduct = Product.Create("Eco T-Shirt", 0m);
        SetId(shirtProduct, shirtProductId);
        Product mugProduct = Product.Create("Eco Mug", 0m);
        SetId(mugProduct, mugProductId);
        Variant mugVariant = Variant.Create(mugProductId, 0m, 0, "MUG-001");
        SetId(mugVariant, mugVariantId);
        SetVariantProduct(mugVariant, mugProduct);

        SetNavigation(existingBaseItem, bundle, shirtProduct, null);
        SetNavigation(existingVariantItem, bundle, mugProduct, mugVariant);

        CreateBundleRequestDto request = new()
        {
            Name = "Eco Starter Kit",
            Description = "Updated description",
            Price = 1290m,
            Products = new List<CreateBundleProductDto>
            {
                new(shirtProductId, null, 3),
                new(mugProductId, new List<CreateBundleVariantDto>
                {
                    new(mugVariantId, 2)
                }, null),
                new(bottleProductId, null, 1)
            }
        };

        Dictionary<Guid, IReadOnlyCollection<Guid>> variantLookup = new()
        {
            [mugProductId] = new List<Guid> { mugVariantId }
        };

        _productRepository
            .Setup(repo => repo.AreProductsActiveAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productRepository
            .Setup(repo => repo.GetActiveVariantsForProductsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(variantLookup);

        _bundleRepository
            .Setup(repo => repo.GetDetailedByIdTrackingAsync(bundleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundle);

        _bundleRepository
            .Setup(repo => repo.GetDetailedByIdAsync(bundleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildRefreshedBundle(bundleId, request, shirtProductId, mugProductId, mugVariantId, bottleProductId));

        Guid currentUserId = Guid.Parse("019a3aaa-dead-beef-b429-d4def7149999");
        _currentUserService
            .Setup(service => service.UserId)
            .Returns(currentUserId);

        UpdateBundleCommandHandler handler = BuildHandler();

        ApiResponse<BundleResponseDto> response = await handler.Handle(new UpdateBundleCommand(bundleId, request), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.Equal("Eco Starter Kit", bundle.Name);
        Assert.Equal("Updated description", bundle.Description);
        Assert.Equal(1290m, bundle.Price);
        Assert.Equal(currentUserId, bundle.UpdatedBy);

        BundleItem updatedBaseItem = Assert.Single(bundle.Items, item => item.ProductId == shirtProductId && item.VariantId is null);
        Assert.True(updatedBaseItem.IsActive);
        Assert.Equal(3, updatedBaseItem.Quantity);

        BundleItem updatedVariantItem = Assert.Single(bundle.Items, item => item.ProductId == mugProductId && item.VariantId == mugVariantId);
        Assert.True(updatedVariantItem.IsActive);
        Assert.Equal(2, updatedVariantItem.Quantity);

        BundleItem newItem = Assert.Single(bundle.Items, item => item.ProductId == bottleProductId && item.VariantId is null);
        Assert.True(newItem.IsActive);
        Assert.Equal(1, newItem.Quantity);
    }

    private UpdateBundleCommandHandler BuildHandler() => new(
        _bundleRepository.Object,
        _productRepository.Object,
        _unitOfWork.Object,
        _currentUserService.Object,
        _logger.Object);

    private static CreateBundleRequestDto BuildRequest() => new()
    {
        Name = "Eco Starter Kit",
        Description = "Bundle for eco-conscious beginners",
        Price = 1290m,
        Products = new List<CreateBundleProductDto>
        {
            new(Guid.Parse("019a2104-81df-7364-8fa9-61702d30c11a"), null, 1)
        }
    };

    private static void SetNavigation(BundleItem item, Bundle bundle, Product? product, Variant? variant)
    {
        var bundleField = typeof(BundleItem).GetField("<Bundle>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        bundleField?.SetValue(item, bundle);

        var productField = typeof(BundleItem).GetField("<Product>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        productField?.SetValue(item, product);

        var variantField = typeof(BundleItem).GetField("<Variant>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        variantField?.SetValue(item, variant);
    }

    private static Bundle BuildRefreshedBundle(
        Guid bundleId,
        CreateBundleRequestDto request,
        Guid shirtProductId,
        Guid mugProductId,
        Guid mugVariantId,
        Guid bottleProductId)
    {
        Bundle refreshed = Bundle.Create(request.Name, request.Price, request.Description);
        SetId(refreshed, bundleId);

        Product shirtProduct = Product.Create("Eco T-Shirt", 0m);
        SetId(shirtProduct, shirtProductId);

        Product mugProduct = Product.Create("Eco Mug", 0m);
        SetId(mugProduct, mugProductId);

        Product bottleProduct = Product.Create("Eco Bottle", 0m);
        SetId(bottleProduct, bottleProductId);

        Variant mugVariant = Variant.Create(mugProductId, 0m, 0, "MUG-001");
        SetId(mugVariant, mugVariantId);
        SetVariantProduct(mugVariant, mugProduct);

        BundleItem shirtItem = BundleItem.Create(bundleId, 3, shirtProductId);
        SetNavigation(shirtItem, refreshed, shirtProduct, null);

        BundleItem mugItem = BundleItem.Create(bundleId, 2, mugProductId, mugVariantId);
        SetNavigation(mugItem, refreshed, mugProduct, mugVariant);

        BundleItem bottleItem = BundleItem.Create(bundleId, 1, bottleProductId);
        SetNavigation(bottleItem, refreshed, bottleProduct, null);

        refreshed.AddItems(new[] { shirtItem, mugItem, bottleItem });

        return refreshed;
    }

    private static void SetId(object entity, Guid id)
    {
        FieldInfo? idField = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        idField?.SetValue(entity, id);
    }

    private static void SetVariantProduct(Variant variant, Product product)
    {
        FieldInfo? productField = typeof(Variant).GetField("<Product>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        productField?.SetValue(variant, product);
    }
}
