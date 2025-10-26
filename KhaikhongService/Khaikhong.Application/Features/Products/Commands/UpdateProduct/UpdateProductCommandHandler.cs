using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Products.Dtos;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<UpdateProductCommandHandler> logger) : IRequestHandler<UpdateProductCommand, ApiResponse<CreateProductResponseDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<UpdateProductCommandHandler> _logger = logger;

    public async Task<ApiResponse<CreateProductResponseDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (request.Request.ProductId != Guid.Empty && request.ProductId != request.Request.ProductId)
        {
            return ApiResponse<CreateProductResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "productId", error = "Route id does not match body id." }
                });
        }

        Guid productId = request.ProductId;

        _logger.LogInformation("Loading product {ProductId} for update", productId);

        Product? product = await _productRepository.GetDetailedByIdTrackingAsync(productId, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product {ProductId} not found for update", productId);
            return ApiResponse<CreateProductResponseDto>.Fail(
                status: 404,
                message: "Product not found",
                errors: new[]
                {
                    new { field = "productId", error = "Product does not exist." }
                });
        }

        Guid? currentUserId = _currentUserService.UserId;

        IReadOnlyCollection<ProductOptionDto> requestOptions = request.Request.Options ?? Array.Empty<ProductOptionDto>();
        IReadOnlyCollection<ProductVariantDto> requestVariants = request.Request.Variants ?? Array.Empty<ProductVariantDto>();

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Applying updates to product {ProductId}", productId);

            product.UpdateDetails(
                request.Request.Name,
                request.Request.BasePrice,
                request.Request.Description,
                request.Request.Sku,
                request.Request.BaseStock);

            if (currentUserId.HasValue)
            {
                product.SetUpdatedBy(currentUserId.Value);
            }

            ApplyOptionUpdates(product, requestOptions);

            Dictionary<string, VariantOption> activeOptionLookup = product.Options
                .Where(option => option.IsActive)
                .ToDictionary(option => option.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            ApplyVariantUpdates(product, requestVariants, activeOptionLookup, currentUserId);

            if (requestVariants.Count > 0)
            {
                product.SetBaseStock(null);
            }
            else
            {
                product.SetBaseStock(request.Request.BaseStock);
            }

            await _unitOfWork.CompleteAsync();
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} updated successfully", productId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error updating product {ProductId}", productId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        CreateProductResponseDto responseDto = _mapper.Map<CreateProductResponseDto>(product);

        return ApiResponse<CreateProductResponseDto>.Success(
            status: 200,
            message: "Product updated successfully",
            data: responseDto);
    }

    private static void ApplyOptionUpdates(Product product, IReadOnlyCollection<ProductOptionDto> requestOptions)
    {
        Dictionary<string, ProductOptionDto> optionLookup = requestOptions
            .ToDictionary(option => option.Name.Trim(), StringComparer.OrdinalIgnoreCase);

        HashSet<string> processedOptions = new(StringComparer.OrdinalIgnoreCase);

        foreach (VariantOption option in product.Options)
        {
            if (!optionLookup.TryGetValue(option.Name.Trim(), out ProductOptionDto? optionDto))
            {
                option.Deactivate();
                continue;
            }

            option.Activate();
            option.UpdateName(optionDto.Name);
            processedOptions.Add(optionDto.Name.Trim());

            Dictionary<string, VariantOptionValue> existingValues = option.Values
                .ToDictionary(value => value.Value.Trim(), StringComparer.OrdinalIgnoreCase);

            List<VariantOptionValue> updatedValues = new();

            foreach (string valueName in optionDto.Values ?? Array.Empty<string>())
            {
                string trimmedValue = valueName.Trim();
                if (existingValues.TryGetValue(trimmedValue, out VariantOptionValue? existingValue))
                {
                    updatedValues.Add(existingValue);
                }
                else
                {
                    VariantOptionValue newValue = VariantOptionValue.Create(option.Id, trimmedValue);
                    updatedValues.Add(newValue);
                }
            }

            option.ReplaceValues(updatedValues);
        }

        IEnumerable<ProductOptionDto> newOptions = requestOptions
            .Where(option => !processedOptions.Contains(option.Name.Trim()));

        List<VariantOption> createdOptions = new();

        foreach (ProductOptionDto optionDto in newOptions)
        {
            VariantOption newOption = VariantOption.Create(product.Id, optionDto.Name);
            List<VariantOptionValue> values = (optionDto.Values ?? Array.Empty<string>())
                .Select(value => VariantOptionValue.Create(newOption.Id, value.Trim()))
                .ToList();

            newOption.ReplaceValues(values);
            createdOptions.Add(newOption);
        }

        if (createdOptions.Count > 0)
        {
            product.AddOptions(createdOptions);
        }
    }

    private static void ApplyVariantUpdates(
        Product product,
        IReadOnlyCollection<ProductVariantDto> requestVariants,
        Dictionary<string, VariantOption> optionLookup,
        Guid? currentUserId)
    {
        Dictionary<string, ProductVariantDto> variantLookup = requestVariants
            .ToDictionary(BuildVariantKey, StringComparer.OrdinalIgnoreCase);

        HashSet<string> processedKeys = new(StringComparer.OrdinalIgnoreCase);

        foreach (Variant variant in product.Variants)
        {
            string key = BuildVariantKey(variant);

            if (!variantLookup.TryGetValue(key, out ProductVariantDto? variantDto))
            {
                variant.Deactivate();
                continue;
            }

            processedKeys.Add(key);
            variant.Activate();
            variant.UpdatePricing(variantDto.Price, variantDto.Sku);
            variant.UpdateInventory(variantDto.Stock);

            if (currentUserId.HasValue)
            {
                variant.SetUpdatedBy(currentUserId.Value);
            }

            IReadOnlyCollection<ProductVariantCombination> combinations = BuildCombinations(variantDto, variant.Id, optionLookup);
            variant.ReplaceCombinations(combinations);
        }

        IEnumerable<ProductVariantDto> newVariants = variantLookup
            .Where(pair => !processedKeys.Contains(pair.Key))
            .Select(pair => pair.Value);

        List<Variant> createdVariants = new();

        foreach (ProductVariantDto variantDto in newVariants)
        {
            Variant variant = Variant.Create(product.Id, variantDto.Price, variantDto.Stock, variantDto.Sku);

            if (currentUserId.HasValue)
            {
                variant.SetCreatedBy(currentUserId.Value);
            }

            IReadOnlyCollection<ProductVariantCombination> combinations = BuildCombinations(variantDto, variant.Id, optionLookup);
            variant.AddCombinations(combinations);

            createdVariants.Add(variant);
        }

        if (createdVariants.Count > 0)
        {
            product.AddVariants(createdVariants);
        }
    }

    private static IReadOnlyCollection<ProductVariantCombination> BuildCombinations(
        ProductVariantDto variantDto,
        Guid variantId,
        Dictionary<string, VariantOption> optionLookup)
    {
        List<ProductVariantCombination> combinations = new();

        foreach (VariantSelectionDto selection in variantDto.Selections ?? Array.Empty<VariantSelectionDto>())
        {
            string optionKey = selection.OptionName.Trim();
            string valueName = selection.Value.Trim();

            if (!optionLookup.TryGetValue(optionKey, out VariantOption? option))
            {
                throw new InvalidOperationException($"Option '{selection.OptionName}' is not defined on the product.");
            }

            VariantOptionValue? value = option.Values.FirstOrDefault(v => string.Equals(v.Value, valueName, StringComparison.OrdinalIgnoreCase));

            if (value is null)
            {
                value = VariantOptionValue.Create(option.Id, valueName);
                option.AddValues(new[] { value });
            }
            ProductVariantCombination combination = ProductVariantCombination.Create(variantId, value.Id);
            combination.Activate();
            combinations.Add(combination);
        }

        return combinations;
    }

    private static string BuildVariantKey(ProductVariantDto variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.Sku))
        {
            return variant.Sku.Trim();
        }

        IEnumerable<string> parts = (variant.Selections ?? Array.Empty<VariantSelectionDto>())
            .Select(selection => $"{selection.OptionName.Trim()}:{selection.Value.Trim()}")
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);

        return string.Join("|", parts);
    }

    private static string BuildVariantKey(Variant variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.Sku))
        {
            return variant.Sku.Trim();
        }

        IEnumerable<string> parts = variant.Combinations
            .Select(combination =>
            {
                VariantOptionValue optionValue = combination.OptionValue;
                string optionName = optionValue.Option?.Name ?? string.Empty;
                return $"{optionName}:{optionValue.Value}";
            })
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);

        return string.Join("|", parts);
    }
}
