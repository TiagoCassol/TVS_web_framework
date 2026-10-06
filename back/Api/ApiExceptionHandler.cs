using BeachTennis.Application.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BeachAula4.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidacaoException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio não atendida"),
            NaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflitoException => (StatusCodes.Status409Conflict, "Conflito"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            _ => (0, string.Empty)
        };

        if (status == 0)
        {
            logger.LogError(exception, "Erro não tratado ao processar a requisição.");
            return false;
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            },
            cancellationToken);
        return true;
    }
}
