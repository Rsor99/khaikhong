using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Application.Features.Bundles.Queries.GetBundleById;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using static Khaikhong.Tests.Features.Bundles.Queries.BundleQueryTestData;

namespace Khaikhong.Tests.Features.Bundles.Queries.GetBundleById;

public sealed class GetBundleByIdQueryHandlerTests
{
    private readonly Mock<IBundleRepository> _bundleRepository = new();
    private readonly Mock<ILogger<GetBundleByIdQueryHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldReturnBundle_WhenFound()
    {
        Bundle bundle = BuildBundleAggregate();

        _bundleRepository
            .Setup(repository => repository.GetDetailedByIdAsync(bundle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundle);

        GetBundleByIdQueryHandler handler = new(_bundleRepository.Object, _logger.Object);

        ApiResponse<BundleResponseDto> response = await handler.Handle(new GetBundleByIdQuery(bundle.Id), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.NotNull(response.Data);
        Assert.Equal(bundle.Id, response.Data.Id);
        Assert.Equal(2, response.Data.Products.Count);
        Assert.Equal(3, response.Data.AvailableBundles);
        Assert.Equal(40m, response.Data.Savings);

        BundleResponseProductDto shirt = Assert.Single(response.Data.Products, product => product.ProductId == ShirtProductId);
        Assert.Null(shirt.Variants);
        Assert.Equal(2, shirt.Quantity);

        BundleResponseProductDto mug = Assert.Single(response.Data.Products, product => product.ProductId == MugProductId);
        Assert.NotNull(mug.Variants);
        BundleResponseVariantDto variant = Assert.Single(mug.Variants!);
        Assert.Equal(MugVariantId, variant.VariantId);
        Assert.Equal("MUG-001", variant.Sku);
        Assert.Equal(1, variant.Quantity);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBundleMissing()
    {
        Guid bundleId = Guid.CreateVersion7();

        _bundleRepository
            .Setup(repository => repository.GetDetailedByIdAsync(bundleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bundle?)null);

        GetBundleByIdQueryHandler handler = new(_bundleRepository.Object, _logger.Object);

        ApiResponse<BundleResponseDto> response = await handler.Handle(new GetBundleByIdQuery(bundleId), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(404, response.Status);
        Assert.Null(response.Data);
    }
}
