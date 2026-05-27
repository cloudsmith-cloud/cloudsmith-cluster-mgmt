// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using FluentMigrator;

namespace CloudSmith.ClusterMgmt.Migrations;

/// <summary>
/// Permanent corrective migration for AB#2346 / BUG-2.
///
/// Ensures cluster_mgmt.clusters has cluster_type, relay_id, and a nullable site_id
/// regardless of VersionInfo state. M20260523001 already adds these columns idempotently,
/// but if the table is recreated outside of FluentMigrator (e.g. fresh schema after a
/// partial reset) while VersionInfo still records M20260523001 as run, the columns will
/// be missing and FluentMigrator won't re-run that migration.
///
/// This migration uses unconditional ADD COLUMN IF NOT EXISTS and ALTER COLUMN DROP NOT NULL
/// so it converges the table to the expected shape on every run. Safe on both fresh and
/// migrated databases.
/// </summary>
[Migration(20260527001, "Ensure cluster_type + relay_id + nullable site_id on cluster_mgmt.clusters (AB#2346)")]
public sealed class M20260527001_EnsureClusterTypeAndRelayId : Migration
{
    public override void Up()
    {
        // Guard: only run if cluster_mgmt.clusters exists. On a fresh install where the
        // API runs this assembly's migrations before M20260519004 (which creates the table),
        // skip the corrective step — M20260519004 + M20260523001 will produce the correct
        // shape on first install.
        Execute.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'cluster_mgmt' AND table_name = 'clusters'
                ) THEN
                    -- cluster_type column (HyperV / AzureLocal / WSFC)
                    ALTER TABLE cluster_mgmt.clusters
                        ADD COLUMN IF NOT EXISTS cluster_type text
                            CHECK (cluster_type IN ('HyperV','AzureLocal','WSFC'));

                    -- relay_id column with FK to core.relays
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'core' AND table_name = 'relays'
                    ) THEN
                        ALTER TABLE cluster_mgmt.clusters
                            ADD COLUMN IF NOT EXISTS relay_id uuid
                                REFERENCES core.relays (relay_id) ON DELETE SET NULL;
                    ELSE
                        ALTER TABLE cluster_mgmt.clusters
                            ADD COLUMN IF NOT EXISTS relay_id uuid;
                    END IF;

                    CREATE INDEX IF NOT EXISTS idx_clusters_relay_id
                        ON cluster_mgmt.clusters (relay_id)
                        WHERE relay_id IS NOT NULL;

                    -- Drop NOT NULL on site_id (relay-registered clusters have no site)
                    ALTER TABLE cluster_mgmt.clusters
                        ALTER COLUMN site_id DROP NOT NULL;
                END IF;
            END $$;
            """);
    }

    public override void Down()
    {
        // Forward-only per CloudSmith migration policy.
    }
}
