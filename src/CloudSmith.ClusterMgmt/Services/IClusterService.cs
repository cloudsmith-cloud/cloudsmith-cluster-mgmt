// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.ClusterMgmt.Models;

namespace CloudSmith.ClusterMgmt.Services;

public interface IClusterService
{
    Task<Guid> RegisterClusterAsync(RegisterClusterRequest request, CancellationToken ct = default);
    Task<ClusterDetail?> GetClusterAsync(Guid clusterId, Guid orgId, CancellationToken ct = default);
    Task<IReadOnlyList<ClusterSummary>> ListClustersAsync(Guid orgId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid clusterId, Guid orgId, ClusterStatus status, CancellationToken ct = default);
    Task DeregisterAsync(Guid clusterId, Guid orgId, CancellationToken ct = default);
}
