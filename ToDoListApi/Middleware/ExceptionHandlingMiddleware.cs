using System.Net;
using System.Text.Json;
using ToDoListApi.Exceptions;

namespace ToDoListApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, exception.Message),
                ForbiddenException => (HttpStatusCode.Forbidden, exception.Message),
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
                _ => (HttpStatusCode.InternalServerError, "Beklenmedik bir hata oluştu.")
            };

            // Beklenmedik (500) hataları detaylıca logluyoruz, kullanıcıya asla sızdırmıyoruz
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Beklenmedik bir hata oluştu. TraceId: {TraceId}", context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning("İşlenen hata: {Title}. TraceId: {TraceId}", title, context.TraceIdentifier);
            }

            var problemDetails = new
            {
                type = $"https://tools.ietf.org/html/rfc9110#section-{GetSectionForStatusCode((int)statusCode)}",
                title,
                status = (int)statusCode,
                traceId = context.TraceIdentifier
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }

        private static string GetSectionForStatusCode(int statusCode) => statusCode switch
        {
            400 => "15.5.1",
            401 => "15.5.2",
            403 => "15.5.4",
            404 => "15.5.5",
            _ => "15.6.1"
        };
    }
}