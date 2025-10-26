using System;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Products.Commands.UpdateProduct;
using Khaikhong.Application.Features.Products.Dtos;
using Microsoft.Extensions.Logging;
using Moq;

namespace Khaikhong.Tests.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<AutoMapper.IMapper> _mapper = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<UpdateProductCommandHandler>> _logger = new();

    public UpdateProductCommandHandlerTests()
    {
        Mock<IUnitOfWorkTransaction> transactionMock = new();
        transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _unitOfWork.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenRouteDoesNotMatchBody()
    {
        UpdateProductRequestDto request = new()
        {
            ProductId = Guid.NewGuid(),
            Name = "Sample",
            BasePrice = 10
        };

        UpdateProductCommandHandler handler = BuildHandler();

        ApiResponse<CreateProductResponseDto> response = await handler.Handle(
            new UpdateProductCommand(Guid.NewGuid(), request),
            CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        _productRepository.Verify(repository => repository.GetDetailedByIdTrackingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductMissing()
    {
        Guid productId = Guid.NewGuid();
        UpdateProductRequestDto request = new()
        {
            ProductId = productId,
            Name = "Sample",
            BasePrice = 10
        };

        _productRepository
            .Setup(repository => repository.GetDetailedByIdTrackingAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Product?)null);

        UpdateProductCommandHandler handler = BuildHandler();

        ApiResponse<CreateProductResponseDto> response = await handler.Handle(new UpdateProductCommand(productId, request), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(404, response.Status);
    }

    private UpdateProductCommandHandler BuildHandler() => new(
        _productRepository.Object,
        _unitOfWork.Object,
        _mapper.Object,
        _currentUserService.Object,
        _logger.Object);
}
