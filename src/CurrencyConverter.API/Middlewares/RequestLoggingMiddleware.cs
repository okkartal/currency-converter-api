using System.Diagnostics;
using System.Security.Claims;

namespace CurrencyConverter.API.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var httpMethod = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.Value ?? string.Empty;
        var clientId = context.User?.FindFirst("ClientId")?.Value ?? "unknown";

        var requestInfo = $"{httpMethod} {path}{queryString}";

        try
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled request from {ClientIp} | {Request} | StatusCode: {StatusCode} | Duration: {Elapsed}ms | ClientId: {ClientId}",
                clientIp, requestInfo, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Request failed from {ClientIp} | {Request} | Duration: {Elapsed}ms | ClientId: {ClientId}",
                clientIp, requestInfo, Stopwatch.GetTimestamp(), clientId);

            throw;
        }
    }
}