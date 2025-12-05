using System;
using Domain.Interfaces;
using Infra.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infra.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' was not found.");
        }

        services.AddDbContext<MyNewLittleBankContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(ISpecificationRepository<>), typeof(EfRepository<>));
        services.AddScoped<Func<MyNewLittleBankContext>>(provider => () => provider.GetRequiredService<MyNewLittleBankContext>());
        services.AddScoped<InboxMessageStore>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        return services;
    }
}
