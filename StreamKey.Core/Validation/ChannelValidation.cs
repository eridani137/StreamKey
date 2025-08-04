using FluentValidation;
using StreamKey.Application.DTOs;

namespace StreamKey.Application.Validation;

public class ChannelValidation : AbstractValidator<ChannelDto>
{
    public ChannelValidation()
    {
        RuleFor(x => x.ChannelName)
            .NotEmpty().WithMessage("Нужно указать название канала");
    }
}