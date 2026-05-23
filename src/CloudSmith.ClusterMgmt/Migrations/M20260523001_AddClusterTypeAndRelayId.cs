// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using FluentMigrator;

namespace CloudSmith.ClusterMgmt.Migrations;

[Migration(20260523001, "Add cluster_type + relay_id to cluster_mgmt.clusters (AB#1670 — Relay bridge)")]
public sealed class M20260523001_AddClusterTypeAndRelayId : Migration
{
    public override void Up()
    {
        // The Relay bridge POST /api/v1/clusters needs to record which relay
        // a cluster was registered through and what kind of cluster it is.
        // Both columns are nullable so the legacy register path (no relay,
        // no cluster_type) keeps working without backfill.
        //
        // The FK on relay_id references core.relays which is created in
        // cloudsmith-core/M20260523008_CreateRelaysAndEnrollment.cs. That
        // migration runs against the same database via the API's
        // MigrateCloudSmithDatabase() call before this assembly's runner
        // executes in MigrateAllDatabases() — see Program.cs.
        Execute.Sql("""
            ALTER TABLE cluster_mgmt.clusters
                ADD COLUMN IF NOT EXISTS cluster_type text
                    CHECK (cluster_type IN ('HyperV','AzureLocal','WSFC'));
            """);

        Execute.Sql("""
            ALTER TABLE cluster_mgmt.clusters
                ADD COLUMN IF NOT EXISTS relay_id uuid
                    REFERENCES core.relays (relay_id) ON DELETE SET NULL;
            """);

        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_clusters_relay_id ON cluster_mgmt.clusters (relay_id) WHERE relay_id IS NOT NULL");

        // Relay-registered clusters are not required to belong to a site (siteId is
        // optional in the bridge POST /api/v1/clusters body), so drop the NOT NULL
        // on site_id. Existing rows are unaffected because they already have values.
        Execute.Sql("ALTER TABLE cluster_mgmt.clusters ALTER COLUMN site_id DROP NOT NULL");
    }

    public override void Down()
    {
        // Forward-only — no-op
    }
}
