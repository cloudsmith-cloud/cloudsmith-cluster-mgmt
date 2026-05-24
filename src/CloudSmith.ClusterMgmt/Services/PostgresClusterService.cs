// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.ClusterMgmt.Models;
using Npgsql;

namespace CloudSmith.ClusterMgmt.Services;

public sealed class PostgresClusterService : IClusterService
{
    private readonly NpgsqlDataSource _db;

    public PostgresClusterService(NpgsqlDataSource db) => _db = db;

    public async Task<Guid> RegisterClusterAsync(RegisterClusterRequest req, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await using var cmd = _db.CreateCommand("""
            INSERT INTO cluster_mgmt.clusters
                (cluster_id, org_id, site_id, name, description, hyperv_version, windows_server_version, status)
            VALUES ($1, $2, $3, $4, $5, $6, $7, 'unknown')
            """);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(req.OrgId);
        cmd.Parameters.AddWithValue(req.SiteId);
        cmd.Parameters.AddWithValue(req.Name);
        cmd.Parameters.AddWithValue((object?)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.HyperVVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.WindowsServerVersion ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
        return id;
    }

    public async Task<ClusterDetail?> GetClusterAsync(Guid clusterId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT cluster_id, org_id, site_id, name, description, hyperv_version,
                   windows_server_version, status, registered_at, last_health_check
            FROM cluster_mgmt.clusters
            WHERE cluster_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(clusterId);
        cmd.Parameters.AddWithValue(orgId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return MapDetail(reader);
    }

    public async Task<IReadOnlyList<ClusterSummary>> ListClustersAsync(Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT c.cluster_id, c.org_id, c.name,
                   s.name AS site_name,
                   (SELECT COUNT(*) FROM cluster_mgmt.nodes n WHERE n.cluster_id = c.cluster_id) AS node_count,
                   c.status, c.registered_at
            FROM cluster_mgmt.clusters c
            LEFT JOIN core.sites s ON s.site_id = c.site_id
            WHERE c.org_id = $1
            ORDER BY c.name
            """);
        cmd.Parameters.AddWithValue(orgId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ClusterSummary>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ClusterSummary(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                (int)reader.GetInt64(4),
                Enum.Parse<ClusterStatus>(reader.GetString(5), ignoreCase: true),
                reader.GetFieldValue<DateTimeOffset>(6)));
        }
        return list;
    }

    public async Task UpdateStatusAsync(Guid clusterId, Guid orgId, ClusterStatus status, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            UPDATE cluster_mgmt.clusters
            SET status = $3, last_health_check = now()
            WHERE cluster_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(clusterId);
        cmd.Parameters.AddWithValue(orgId);
        cmd.Parameters.AddWithValue(status.ToString().ToLowerInvariant());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeregisterAsync(Guid clusterId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            DELETE FROM cluster_mgmt.clusters WHERE cluster_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(clusterId);
        cmd.Parameters.AddWithValue(orgId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static ClusterDetail MapDetail(NpgsqlDataReader r) => new(
        r.GetGuid(0),
        r.GetGuid(1),
        r.GetGuid(2),
        r.GetString(3),
        r.IsDBNull(4) ? null : r.GetString(4),
        r.IsDBNull(5) ? null : r.GetString(5),
        r.IsDBNull(6) ? null : r.GetString(6),
        Enum.Parse<ClusterStatus>(r.GetString(7), ignoreCase: true),
        r.GetFieldValue<DateTimeOffset>(8),
        r.IsDBNull(9) ? null : r.GetFieldValue<DateTimeOffset>(9));
}
