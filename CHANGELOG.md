# Changelog

All notable changes to **cloudsmith-cluster-mgmt** will be documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-05-23

### Added

- `cluster_type` column on the clusters table with a `CHECK` constraint restricting values to `HyperV`, `AzureLocal`, or `WSFC`.
- `relay_id` foreign key on clusters that links a registered cluster to the on-prem Relay that owns it.

### Changed

- `site_id` is now nullable so cluster registration can defer site assignment until the operator chooses a site in the portal.
