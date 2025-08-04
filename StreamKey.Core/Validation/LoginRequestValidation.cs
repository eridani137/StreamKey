using System.Data;
using FluentValidation;
using Microsoft.AspNetCore.Identity.Data;

namespace StreamKey.Application.Validation;

public class LoginRequestValidation : AbstractValidator<LoginRequest>
{
    public LoginRequestValidation()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Нужно ввести логин");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Нужно ввести пароль")
            .MinimumLength(8).WithMessage("Пароль должен содержать не менее 8 символов")
            .Matches("[A-Z]").WithMessage("Пароль должен содержать хотя бы одну заглавную букву")
            .Matches("[a-z]").WithMessage("Пароль должен содержать хотя бы одну строчную букву")
            .Matches("[0-9]").WithMessage("Пароль должен содержать хотя бы одну цифру")
            .Matches("[^a-zA-Z0-9]").WithMessage("Пароль должен содержать хотя бы один специальный символ");
    }
}