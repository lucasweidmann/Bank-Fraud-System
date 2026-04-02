using System.Net;
using System.Text.Json;

namespace BankFraudSystem.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            KeyNotFoundException     => (HttpStatusCode.NotFound,            ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,     ex.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest,         ex.Message),
            ArgumentException        => (HttpStatusCode.BadRequest,          ex.Message),
            _                        => (HttpStatusCode.InternalServerError, "Erro interno do servidor.")
        };

        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new { error = message });
        return context.Response.WriteAsync(body);
    }
}
