// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace CloudSmith.ClusterMgmt.Tests.Migrations;

public sealed class ClusterMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _pg.StartAsync();
    public Task DisposeAsync()    => _pg.DisposeAsync().AsTask();

    private IServiceProvider BuildServices()
    {
        return new ServiceCollection()
            .AddSingleton(NpgsqlDataSource.Create(_pg.GetConnectionString()))
            .AddCloudSmithClusterMgmt(_pg.GetConnectionString())
            .BuildServiceProvider();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migration_CreatesAllClusterMgmtTables()
    {
        // cloudsmith-core migrations must run first to create core.sites
        // In integration tests we seed the prerequisite schema manually
        await using var conn = new NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var setup = conn.CreateCommand();
        setup.CommandText = """
            CREATE SCHEMA IF NOT EXISTS core;
            CREATE TABLE IF NOT EXISTS core.sites (
                site_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                org_id  UUID NOT NULL,
                name    TEXT NOT NULL
            );
            """;
        await setup.ExecuteNonQueryAsync();

        var services = BuildServices();
        services.MigrateClusterMgmtDatabase();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'cluster_mgmt'
            ORDER BY table_name
            """;
        await using var reader = await cmd.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));

        Assert.Contains("clusters",         tables);
        Assert.Contains("nodes",            tables);
        Assert.Contains("health_snapshots", tables);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migration_IsIdempotent()
    {
        await using var conn = new NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var setup = conn.CreateCommand();
        setup.CommandText = """
            CREATE SCHEMA IF NOT EXISTS core;
            CREATE TABLE IF NOT EXISTS core.sites (
                site_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                org_id  UUID NOT NULL,
                name    TEXT NOT NULL
            );
            """;
        await setup.ExecuteNonQueryAsync();

        var services = BuildServices();
        // Run twice — should not throw
        services.MigrateClusterMgmtDatabase();
        services.MigrateClusterMgmtDatabase();
    }
}
