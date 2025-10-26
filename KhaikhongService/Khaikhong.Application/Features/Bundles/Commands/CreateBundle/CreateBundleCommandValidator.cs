using System;
using System.Linq;
using FluentValidation;
using Khaikhong.Application.Features.Bundles.Dtos;

namespace Khaikhong.Application.Features.Bundles.Commands.CreateBundle;

public sealed class CreateBundleCommandValidator : AbstractValidator<CreateBundleCommand>
{
    public CreateBundleCommandValidator()
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(new CreateBundleRequestValidator());
    }

    private sealed class CreateBundleRequestValidator : AbstractValidator<CreateBundleRequestDto>
    {
        public CreateBundleRequestValidator()
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

                // --- ส่วนที่แก้ไข: ปรับปรุงเงื่อนไข Quantity/Variants ---
                product.RuleFor(p => p)
                    .Must(p =>
                        // กรณีที่ 1: มี Variants ถูกระบุ
                        (p.Variants is { Count: > 0 }) ||
                        // กรณีที่ 2: ไม่มี Variants ถูกระบุ -> ต้องมี Quantity ของ Product และ Quantity ต้อง > 0
                        (p.Variants is null || p.Variants.Count == 0) && (p.Quantity.HasValue && p.Quantity.Value > 0)
                    )
                    .WithMessage(p =>
                        $"Product {p.ProductId} must have a positive quantity defined either at the product level (when no variants are used) or within the variants list."
                    );

                // RuleFor Quantity ถูกลบออกเนื่องจากถูกรวมใน Must() ด้านบนแล้ว

                // RuleFor Variants ยังคงเดิม
                product.RuleFor(p => p.Variants)
                    .Must(variants =>
                        variants is null || variants.Select(variant => variant.VariantId).Distinct().Count() ==
                        variants.Count)
                    .WithMessage("Duplicate variant ids are not allowed per product.");

                // RuleForEach Variants ยังคงเดิม
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
}
