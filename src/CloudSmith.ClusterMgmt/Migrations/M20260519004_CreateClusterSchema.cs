// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using FluentMigrator;

namespace CloudSmith.ClusterMgmt.Migrations;

[Migration(20260519004)]
public sealed class M20260519004_CreateClusterSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS cluster_mgmt");

        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS cluster_mgmt.clusters (
                cluster_id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                org_id                  UUID NOT NULL,
                site_id                 UUID NOT NULL REFERENCES core.sites(site_id) ON DELETE CASCADE,
                name                    TEXT NOT NULL,
                description             TEXT,
                hyperv_version          TEXT,
                windows_server_version  TEXT,
                status                  TEXT NOT NULL DEFAULT 'unknown',
                registered_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
                last_health_check       TIMESTAMPTZ,
                UNIQUE (org_id, name)
            )
            """);

        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_clusters_org_id ON cluster_mgmt.clusters (org_id)");
        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_clusters_site_id ON cluster_mgmt.clusters (site_id)");

        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS cluster_mgmt.nodes (
                node_id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                cluster_id              UUID NOT NULL REFERENCES cluster_mgmt.clusters(cluster_id) ON DELETE CASCADE,
                org_id                  UUID NOT NULL,
                name                    TEXT NOT NULL,
                ip_address              TEXT,
                fqdn                    TEXT,
                hyperv_version          TEXT,
                logical_processor_count INT,
                total_memory_bytes      BIGINT,
                status                  TEXT NOT NULL DEFAULT 'unknown',
                registered_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
                last_seen               TIMESTAMPTZ,
                UNIQUE (cluster_id, name)
            )
            """);

        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_nodes_cluster_id ON cluster_mgmt.nodes (cluster_id)");
        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_nodes_org_id     ON cluster_mgmt.nodes (org_id)");

        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS cluster_mgmt.health_snapshots (
                snapshot_id     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                cluster_id      UUID NOT NULL REFERENCES cluster_mgmt.clusters(cluster_id) ON DELETE CASCADE,
                org_id          UUID NOT NULL,
                captured_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
                status          TEXT NOT NULL,
                node_count      INT  NOT NULL DEFAULT 0,
                online_count    INT  NOT NULL DEFAULT 0,
                details         JSONB
            )
            """);

        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_health_cluster_id ON cluster_mgmt.health_snapshots (cluster_id, captured_at DESC)");
    }

    public override void Down() { }
}
