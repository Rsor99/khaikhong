using AutoMapper;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Products.Dtos;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Khaikhong.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(CreateProductRequestDto Request) : IRequest<ApiResponse<CreateProductResponseDto>>;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<CreateProductCommandHandler> logger) : IRequestHandler<CreateProductCommand, ApiResponse<CreateProductResponseDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<CreateProductCommandHandler> _logger = logger;

    public async Task<ApiResponse<CreateProductResponseDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        CreateProductRequestDto payload = request.Request;

        var (nameExists, skuExists) = await _productRepository.ExistsByNameOrSkuAsync(
            payload.Name,
            payload.Sku,
            cancellationToken);

        if (nameExists)
        {
            _logger.LogWarning("Product creation blocked due to duplicate name {ProductName}", payload.Name);
            return ApiResponse<CreateProductResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "Request.Name", error = "Product name already exists." }
                });
        }

        if (skuExists)
        {
            _logger.LogWarning("Product creation blocked due to duplicate SKU {ProductSku}", payload.Sku);
            return ApiResponse<CreateProductResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "Request.Sku", error = "Product SKU already exists." }
                });
        }

        Product product = _mapper.Map<Product>(payload);

        Guid? currentUserId = _currentUserService.UserId;
        if (currentUserId.HasValue)
        {
            product.SetCreatedBy(currentUserId.Value);
        }

        IReadOnlyCollection<ProductOptionDto> optionDtos = payload.Options ?? Array.Empty<ProductOptionDto>();
        IReadOnlyCollection<ProductVariantDto> variantDtos = payload.Variants ?? Array.Empty<ProductVariantDto>();

        var optionAggregates = optionDtos
            .Select(optionDto =>
            {
                VariantOption option = VariantOption.Create(product.Id, optionDto.Name);
                var values = (optionDto.Values ?? Array.Empty<string>())
                    .Select(value => VariantOptionValue.Create(option.Id, value))
                    .ToList();

                option.AddValues(values);
                return new
                {
                    Option = option,
                    Values = values
                };
            })
            .ToList();

        if (optionAggregates.Count > 0)
        {
            product.AddOptions(optionAggregates.Select(option => option.Option));
        }

        Dictionary<string, VariantOptionValue> optionValueLookup = optionAggregates
            .SelectMany(option => option.Values.Select(value => new
            {
                OptionName = option.Option.Name,
                Value = value.Value,
                OptionValue = value
            }))
            .ToDictionary(
                keySelector: entry => BuildOptionValueKey(entry.OptionName, entry.Value),
                elementSelector: entry => entry.OptionValue,
                comparer: StringComparer.OrdinalIgnoreCase);

        List<object> missingSelections = variantDtos
            .Select((variant, variantIndex) => new { variant, variantIndex })
            .SelectMany(tuple => (tuple.variant.Selections ?? Array.Empty<VariantSelectionDto>()).Select(selection => new
            {
                tuple.variantIndex,
                selection.OptionName,
                selection.Value,
                Key = BuildOptionValueKey(selection.OptionName, selection.Value)
            }))
            .Where(selection => !optionValueLookup.ContainsKey(selection.Key))
            .Select(selection => new
            {
                field = $"Request.Variants[{selection.variantIndex}].Selections",
                error = $"Option '{selection.OptionName}' value '{selection.Value}' is not defined."
            })
            .Cast<object>()
            .ToList();

        if (missingSelections.Count > 0)
        {
            return ApiResponse<CreateProductResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: missingSelections.ToArray());
        }

        var variantAggregates = variantDtos
            .Select(variantDto =>
            {
                Variant variant = Variant.Create(product.Id, variantDto.Price, variantDto.Stock, variantDto.Sku);

                if (currentUserId.HasValue)
                {
                    variant.SetCreatedBy(currentUserId.Value);
                }

                var combinations = (variantDto.Selections ?? Array.Empty<VariantSelectionDto>())
                    .Select(selection => optionValueLookup[BuildOptionValueKey(selection.OptionName, selection.Value)])
                    .Select(value => ProductVariantCombination.Create(variant.Id, value.Id))
                    .ToList();

                variant.AddCombinations(combinations);

                return variant;
            })
            .ToList();

        if (variantAggregates.Count > 0)
        {
            product.AddVariants(variantAggregates);
            product.SetBaseStock(null);
        }
        else
        {
            product.SetBaseStock(payload.BaseStock);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _productRepository.BulkInsertAsync(product, cancellationToken);
            await _unitOfWork.CompleteAsync();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(exception, "Failed to create product {ProductName}", payload.Name);
            throw;
        }

        CreateProductResponseDto responseDto = _mapper.Map<CreateProductResponseDto>(product);
        _logger.LogInformation("Product {ProductId} created successfully", responseDto.Id);

        return ApiResponse<CreateProductResponseDto>.Success(
            status: 200,
            message: "Product created successfully",
            data: responseDto);
    }

    private static string BuildOptionValueKey(string optionName, string value)
    {
        StringBuilder builder = new(optionName.Length + value.Length + 1);
        return builder
            .Append(optionName.Trim())
            .Append('|')
            .Append(value.Trim())
            .ToString();
    }
}
