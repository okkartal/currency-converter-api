namespace CurrencyConverter.API.Middlewares;

public class ApiVersionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiVersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString();

        context.Items["ApiVersion"] = path.Contains("/api/v1") ? "v1" :
            path.Contains("/api/v2") ? "v2" : "unknown";

        await _next(context);
    }
}