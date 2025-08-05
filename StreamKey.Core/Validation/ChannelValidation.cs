using FluentValidation;
using StreamKey.Core.DTOs;

namespace StreamKey.Core.Validation;

public class ChannelValidation : AbstractValidator<ChannelDto>
{
    public ChannelValidation()
    {
        RuleFor(x => x.ChannelName)
            .NotEmpty().WithMessage("Нужно указать название канала");

        RuleFor(x => x.Position)
            .InclusiveBetween(0, 20)
            .WithMessage("Значение позиции должно быть в пределах 0-20");
    }
}