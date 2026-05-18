using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SourceRegistryService.API.Auth;
using SourceRegistryService.API.Middleware;
using SourceRegistryService.API.Services;
using SourceRegistryService.Application.Extensions;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Infrastructure.Data;
using SourceRegistryService.Infrastructure.Extensions;
using SourceRegistryService.Infrastructure.SeedData;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        if (args.Contains("--import-from-filesystem"))
        {
            var dataAcquisitionRoot = GetArg(args, "--import-from-filesystem")
                ?? throw new ArgumentException("--import-from-filesystem requires a path argument");
            await RunFilesystemImportAsync(host, dataAcquisitionRoot);
            return;
        }

        await host.RunAsync();
    }

    private static async Task RunFilesystemImportAsync(IHost host, string path)
    {
        using var scope = host.Services.CreateScope();
        var importer = scope.ServiceProvider.GetRequiredService<FilesystemImporter>();
        await importer.ImportAsync(path);
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
            .ConfigureAppConfiguration((_, config) => config.AddEnvironmentVariables());
}

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration) => _configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddLogging(b => { b.AddConsole(); b.AddDebug(); });
        services.AddEndpointsApiExplorer();
        ConfigureSwagger(services);
        ConfigureJwtAuthentication(services);
        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddApplicationServices();
        services.AddInfrastructureServices(_configuration);
        services.AddHttpClient("anthropic");
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<FilesystemImporter>();
    }

    private void ConfigureJwtAuthentication(IServiceCollection services)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured");
        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
            });
    }

    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Source Registry Service API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
        SourceRegistryDbContext context, ILogger<Startup> logger)
    {
        try
        {
            context.Database.Migrate();
            logger.LogInformation("Database migration completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database migration");
            throw;
        }

        // Seed Ukrainian administrative data on first start
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<SourceRegistryDbContext>();
            var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            AdminSeeder.SeedAsync(ctx, seedLogger).GetAwaiter().GetResult();
        }

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCustomErrorHandling();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
