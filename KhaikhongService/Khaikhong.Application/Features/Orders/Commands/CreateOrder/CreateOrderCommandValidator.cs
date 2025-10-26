using FluentValidation;

namespace Khaikhong.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(new CreateOrderRequestValidator());
    }

    private sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequestDto>
    {
        private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "product", "variant", "bundle"
        };

        public CreateOrderRequestValidator()
        {
            RuleFor(dto => dto.Items)
                .NotNull()
                .Must(items => items.Count > 0)
                .WithMessage("At least one item must be included.");

            RuleForEach(dto => dto.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.Id)
                    .NotEmpty();

                item.RuleFor(i => i.Type)
                    .NotEmpty()
                    .Must(type => SupportedTypes.Contains(type))
                    .WithMessage(i => $"Unsupported item type '{i.Type}'.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0);
            });
        }
    }
}
