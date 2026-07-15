using System.Text;
using CEMETRIX.Application.Interfaces.Repositories;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Domain.Entities;
using CEMETRIX.Infrastructure.Email;
using CEMETRIX.Infrastructure.Identity;
using CEMETRIX.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CEMETRIX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new System.InvalidOperationException("DefaultConnection is missing in configuration.");

        services.AddDbContextFactory<ApplicationDbContext>(opts =>
            opts.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<ApplicationDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
        {
            opts.Password.RequireDigit = true;
            opts.Password.RequireLowercase = true;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequiredLength = 8;
            opts.User.RequireUniqueEmail = true;
            opts.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<EmailSettings>(configuration.GetSection("Email"));

        var jwt = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IGmailAddressValidator, GmailAddressValidator>();
        // Transient: each service gets its own DbContext (avoids parallel-query crashes on dashboard + navbar).
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddTransient(typeof(IRepository<>), typeof(Persistence.Repositories.Repository<>));
        services.AddTransient<IGraveRepository, Persistence.Repositories.GraveRepository>();
        services.AddTransient<IDeceasedPersonRepository, Persistence.Repositories.DeceasedPersonRepository>();
        services.AddTransient<IBookingRepository, Persistence.Repositories.BookingRepository>();
        services.AddTransient<INotificationRepository, Persistence.Repositories.NotificationRepository>();
        services.AddTransient<IVisitorRepository, Persistence.Repositories.VisitorRepository>();
        services.AddTransient<IActivityLogRepository, Persistence.Repositories.ActivityLogRepository>();
        services.AddTransient<IAuditLogRepository, Persistence.Repositories.AuditLogRepository>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        var keyBytes = Encoding.UTF8.GetBytes(jwt.Secret);

        services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
            {
                opts.RequireHttpsMetadata = false;
                opts.SaveToken = true;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = System.TimeSpan.FromMinutes(2)
                };
            });
        return services;
    }
}
