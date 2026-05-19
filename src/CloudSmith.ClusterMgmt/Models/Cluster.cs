// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

namespace CloudSmith.ClusterMgmt.Models;

public enum ClusterStatus { Unknown, Healthy, Degraded, Offline }

public sealed record ClusterSummary(
    Guid   ClusterId,
    Guid   OrgId,
    string Name,
    string SiteName,
    int    NodeCount,
    ClusterStatus Status,
    DateTimeOffset RegisteredAt);

public sealed record ClusterDetail(
    Guid   ClusterId,
    Guid   OrgId,
    Guid   SiteId,
    string Name,
    string? Description,
    string? HyperVVersion,
    string? WindowsServerVersion,
    ClusterStatus Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastHealthCheck);

public sealed record RegisterClusterRequest(
    Guid   OrgId,
    Guid   SiteId,
    string Name,
    string? Description,
    string? HyperVVersion,
    string? WindowsServerVersion);
