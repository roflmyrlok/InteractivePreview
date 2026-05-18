using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Infrastructure.Data;
using SourceRegistryService.Infrastructure.Data.Repositories;

namespace SourceRegistryService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SourceRegistryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(SourceRegistryDbContext).Assembly.FullName)));

        services.AddScoped<IOblastRepository, OblastRepository>();
        services.AddScoped<IHromadaRepository, HromadaRepository>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<IDiscoveryRunRepository, DiscoveryRunRepository>();

        return services;
    }
}
