// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

namespace CloudSmith.ClusterMgmt.Models;

public enum NodeStatus { Unknown, Online, Offline, Maintenance, Draining }

public sealed record NodeSummary(
    Guid   NodeId,
    Guid   ClusterId,
    string Name,
    string? IpAddress,
    NodeStatus Status,
    DateTimeOffset RegisteredAt);

public sealed record NodeDetail(
    Guid   NodeId,
    Guid   ClusterId,
    Guid   OrgId,
    string Name,
    string? IpAddress,
    string? Fqdn,
    string? HyperVVersion,
    int?   LogicalProcessorCount,
    long?  TotalMemoryBytes,
    NodeStatus Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastSeen);

public sealed record RegisterNodeRequest(
    Guid   ClusterId,
    Guid   OrgId,
    string Name,
    string? IpAddress,
    string? Fqdn,
    string? HyperVVersion,
    int?   LogicalProcessorCount,
    long?  TotalMemoryBytes);
