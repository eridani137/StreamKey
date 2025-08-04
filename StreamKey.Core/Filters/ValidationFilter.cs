using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace StreamKey.Application.Filters;

public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var entityToValidate = context.GetArgument<T>(0);
        var validationResult = await validator.ValidateAsync(entityToValidate);
        if (!validationResult.IsValid)
        {
            return Microsoft.AspNetCore.Http.Results.ValidationProblem(validationResult.ToDictionary());
        }
        
        return await next(context);
    }
}