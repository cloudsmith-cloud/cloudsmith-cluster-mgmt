// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.ClusterMgmt.Models;
using Npgsql;

namespace CloudSmith.ClusterMgmt.Services;

public sealed class PostgresNodeService : INodeService
{
    private readonly NpgsqlDataSource _db;

    public PostgresNodeService(NpgsqlDataSource db) => _db = db;

    public async Task<Guid> RegisterNodeAsync(RegisterNodeRequest req, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await using var cmd = _db.CreateCommand("""
            INSERT INTO cluster_mgmt.nodes
                (node_id, cluster_id, org_id, name, ip_address, fqdn,
                 hyperv_version, logical_processor_count, total_memory_bytes, status)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, 'unknown')
            """);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(req.ClusterId);
        cmd.Parameters.AddWithValue(req.OrgId);
        cmd.Parameters.AddWithValue(req.Name);
        cmd.Parameters.AddWithValue((object?)req.IpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.Fqdn ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.HyperVVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.LogicalProcessorCount ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.TotalMemoryBytes ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
        return id;
    }

    public async Task<NodeDetail?> GetNodeAsync(Guid nodeId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT node_id, cluster_id, org_id, name, ip_address, fqdn,
                   hyperv_version, logical_processor_count, total_memory_bytes,
                   status, registered_at, last_seen
            FROM cluster_mgmt.nodes
            WHERE node_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(nodeId);
        cmd.Parameters.AddWithValue(orgId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return MapDetail(reader);
    }

    public async Task<IReadOnlyList<NodeSummary>> ListNodesAsync(Guid clusterId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT node_id, cluster_id, name, ip_address, status, registered_at
            FROM cluster_mgmt.nodes
            WHERE cluster_id = $1 AND org_id = $2
            ORDER BY name
            """);
        cmd.Parameters.AddWithValue(clusterId);
        cmd.Parameters.AddWithValue(orgId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<NodeSummary>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new NodeSummary(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                Enum.Parse<NodeStatus>(reader.GetString(4), ignoreCase: true),
                reader.GetFieldValue<DateTimeOffset>(5)));
        }
        return list;
    }

    public async Task UpdateStatusAsync(Guid nodeId, Guid orgId, NodeStatus status, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            UPDATE cluster_mgmt.nodes SET status = $3 WHERE node_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(nodeId);
        cmd.Parameters.AddWithValue(orgId);
        cmd.Parameters.AddWithValue(status.ToString().ToLowerInvariant());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RecordHeartbeatAsync(Guid nodeId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            UPDATE cluster_mgmt.nodes SET last_seen = now(), status = 'online'
            WHERE node_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(nodeId);
        cmd.Parameters.AddWithValue(orgId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DrainNodeAsync(Guid nodeId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            UPDATE cluster_mgmt.nodes SET status = 'draining' WHERE node_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(nodeId);
        cmd.Parameters.AddWithValue(orgId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SetMaintenanceModeAsync(Guid nodeId, Guid orgId, bool enable, CancellationToken ct = default)
    {
        var status = enable ? "maintenance" : "online";
        await using var cmd = _db.CreateCommand("""
            UPDATE cluster_mgmt.nodes SET status = $3 WHERE node_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(nodeId);
        cmd.Parameters.AddWithValue(orgId);
        cmd.Parameters.AddWithValue(status);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeregisterAsync(Guid nodeId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            DELETE FROM cluster_mgmt.nodes WHERE node_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(nodeId);
        cmd.Parameters.AddWithValue(orgId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static NodeDetail MapDetail(NpgsqlDataReader r) => new(
        r.GetGuid(0),
        r.GetGuid(1),
        r.GetGuid(2),
        r.GetString(3),
        r.IsDBNull(4) ? null : r.GetString(4),
        r.IsDBNull(5) ? null : r.GetString(5),
        r.IsDBNull(6) ? null : r.GetString(6),
        r.IsDBNull(7) ? null : r.GetInt32(7),
        r.IsDBNull(8) ? null : r.GetInt64(8),
        Enum.Parse<NodeStatus>(r.GetString(9), ignoreCase: true),
        r.GetFieldValue<DateTimeOffset>(10),
        r.IsDBNull(11) ? null : r.GetFieldValue<DateTimeOffset>(11));
}
