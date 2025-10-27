using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Application.Features.Bundles.Queries.GetAllBundles;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using static Khaikhong.Tests.Features.Bundles.Queries.BundleQueryTestData;

namespace Khaikhong.Tests.Features.Bundles.Queries.GetAllBundles;

public sealed class GetAllBundlesQueryHandlerTests
{
    private readonly Mock<IBundleRepository> _bundleRepository = new();
    private readonly Mock<ILogger<GetAllBundlesQueryHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldReturnBundles()
    {
        Bundle bundle = BuildBundleAggregate();

        _bundleRepository
            .Setup(repository => repository.GetAllDetailedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bundle> { bundle });

        GetAllBundlesQueryHandler handler = new(_bundleRepository.Object, _logger.Object);

        ApiResponse<List<BundleResponseDto>> response = await handler.Handle(new GetAllBundlesQuery(), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.NotNull(response.Data);
        BundleResponseDto bundleDto = Assert.Single(response.Data);
        Assert.Equal(3, bundleDto.AvailableBundles);
        Assert.Equal(40m, bundleDto.Savings);
    }
}
