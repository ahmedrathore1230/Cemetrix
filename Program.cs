using System.Reflection;
using CEMETRIX.API.Middleware;
using CEMETRIX.API.Services;
using CEMETRIX.Application;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Infrastructure;
using CEMETRIX.Infrastructure.Persistence.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/cemetrix-api-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssembly(typeof(CEMETRIX.Application.DependencyInjection).Assembly);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Services.AddApplicationLayer();
    builder.Services.AddInfrastructureLayer(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();

    builder.Services.AddCors(o =>
    {
        o.AddPolicy("DefaultCors", p => p
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowedToAllowWildcardSubdomains());
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CEMETRIX API", Version = "v1", Description = "Enterprise Graveyard Management System" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, System.Array.Empty<string>() }
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        try { await DbSeeder.SeedAsync(scope.ServiceProvider); }
        catch (System.Exception ex) { Log.Warning(ex, "Seeding skipped: {Message}", ex.Message); }
    }

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/swagger/v1/swagger.json", "CEMETRIX API v1");
            o.DocumentTitle = "CEMETRIX API";
        });
    }

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("DefaultCors");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGet("/", () => Results.Redirect("/swagger"));

    app.Run();
}
catch (System.Exception ex)
{
    Log.Fatal(ex, "CEMETRIX API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
