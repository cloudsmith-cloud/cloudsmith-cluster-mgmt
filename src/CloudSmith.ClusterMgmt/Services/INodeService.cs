// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.ClusterMgmt.Models;

namespace CloudSmith.ClusterMgmt.Services;

public interface INodeService
{
    Task<Guid> RegisterNodeAsync(RegisterNodeRequest request, CancellationToken ct = default);
    Task<NodeDetail?> GetNodeAsync(Guid nodeId, Guid orgId, CancellationToken ct = default);
    Task<IReadOnlyList<NodeSummary>> ListNodesAsync(Guid clusterId, Guid orgId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid nodeId, Guid orgId, NodeStatus status, CancellationToken ct = default);
    Task RecordHeartbeatAsync(Guid nodeId, Guid orgId, CancellationToken ct = default);
    Task DeregisterAsync(Guid nodeId, Guid orgId, CancellationToken ct = default);
}
