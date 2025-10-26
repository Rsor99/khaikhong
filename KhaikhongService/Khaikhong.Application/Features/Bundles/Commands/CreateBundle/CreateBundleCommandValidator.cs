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
                .WithMessage("At least one product must be included in the bundle.")
                .Must(products => products.All(p => p.Quantity > 0))
                .WithMessage("Product quantity must be greater than zero.");

            RuleForEach(dto => dto.Products).ChildRules(product =>
            {
                product.RuleFor(p => p.ProductId)
                    .NotEmpty();

                product.RuleFor(p => p.Variants)
                    .Must(variants => variants is null || variants.Distinct().Count() == variants.Count)
                    .WithMessage("Duplicate variant ids are not allowed per product.");
            });
        }
    }
}
