using Microsoft.EntityFrameworkCore;
using SanlamClaims.API;
using SanlamClaims.API.Common;
using SanlamClaims.Application;
using SanlamClaims.Infrastructure;
using SanlamClaims.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var logFolder = builder.Configuration["App:LogFolder"]
        ?? throw new InvalidOperationException("App:LogFolder is not configured.");

    builder.Host.UseSerilog((_, _, config) =>
        config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logFolder, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}"));

    builder.Services.AddPresentationServices();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddAuth(builder.Configuration);
    builder.Services.AddApiRateLimiting(builder.Configuration);

    builder.Services.Configure<HostOptions>(options =>
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Sanlam Claims API v1");
            options.RoutePrefix = "swagger";
        });

        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Skipping automatic migration — no database reachable.");
        }
    }

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
