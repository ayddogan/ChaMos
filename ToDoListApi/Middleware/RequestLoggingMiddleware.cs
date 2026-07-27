using Serilog.Context;
using System.Diagnostics;
using System.Security.Claims;

namespace ToDoListApi.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.TraceIdentifier;

            // Bu blok içindeki TÜM loglara (biz yazsak da, framework yazsa da)
            // otomatik olarak CorrelationId eklenecek (Enrich.FromLogContext sayesinde)
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                var stopwatch = Stopwatch.StartNew();

                await _next(context);

                stopwatch.Stop();

                var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value ?? "Anonim";

                _logger.LogInformation(
                    "İstek tamamlandı: {Method} {Path} | Kullanıcı: {UserEmail} | Durum: {StatusCode} | Süre: {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    userEmail,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                );
            }
        }
    }
}