using FluentValidation;
using StreamKey.Core.DTOs;

namespace StreamKey.Core.Validation;

public class ChannelValidation : AbstractValidator<ChannelDto>
{
    public ChannelValidation()
    {
        RuleFor(x => x.ChannelName)
            .NotEmpty().WithMessage("Нужно указать название канала");
    }
}