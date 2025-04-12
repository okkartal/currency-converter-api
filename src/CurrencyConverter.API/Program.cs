using CurrencyConverter.API.Extensions;
using CurrencyConverter.API.Middlewares;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

//Configure Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/currency-converter-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddApplicationServices(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseMiddleware<ApiVersionMiddleware>();

app.UseHttpsRedirection();

//Add request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();
//Enable rate limiting
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

try
{
    app.Logger.Log(LogLevel.Information, "Starting CurrencyConverter API");
    app.Run();
}
catch (Exception ex)
{
    app.Logger.Log(LogLevel.Critical, $"Fatal error starting CurrencyConverter API {ex.Message}");
}