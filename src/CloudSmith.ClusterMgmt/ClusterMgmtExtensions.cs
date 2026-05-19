// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.ClusterMgmt.Services;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace CloudSmith.ClusterMgmt;

public static class ClusterMgmtExtensions
{
    public static IServiceCollection AddCloudSmithClusterMgmt(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddScoped<IClusterService, PostgresClusterService>();
        services.AddScoped<INodeService, PostgresNodeService>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(ClusterMgmtExtensions).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    public static IServiceProvider MigrateClusterMgmtDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
        return services;
    }
}
