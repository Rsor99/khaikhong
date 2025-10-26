using FluentValidation;
using Khaikhong.Application.Features.Bundles.Dtos;

namespace Khaikhong.Application.Features.Bundles.Commands;

internal sealed class BundleRequestValidator : AbstractValidator<CreateBundleRequestDto>
{
    public BundleRequestValidator()
    {
        RuleFor(dto => dto.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(dto => dto.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(dto => dto.Products)
            .NotNull()
            .Must(products => products.Count > 0)
            .WithMessage("At least one product must be included in the bundle.");

        RuleForEach(dto => dto.Products).ChildRules(product =>
        {
            product.RuleFor(p => p.ProductId)
                .NotEmpty();

            product.RuleFor(p => p.Quantity)
                .Must((p, quantity) => p.Variants is { Count: > 0 } || (quantity.HasValue && quantity.Value > 0))
                .WithMessage(p => $"Quantity must be greater than zero for product {p.ProductId} when no variants are specified.");

            product.RuleFor(p => p.Variants)
                .Must(variants => variants is null || variants.Select(variant => variant.VariantId).Distinct().Count() == variants.Count)
                .WithMessage("Duplicate variant ids are not allowed per product.");

            product.RuleForEach(p => p.Variants)
                .ChildRules(variant =>
                {
                    variant.RuleFor(v => v.VariantId)
                        .NotEmpty();

                    variant.RuleFor(v => v.Quantity)
                        .GreaterThan(0)
                        .WithMessage(v => $"Variant {v.VariantId} must have a quantity greater than zero.");
                });
        });
    }
}
