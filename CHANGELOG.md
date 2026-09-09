# Release notes

## Unreleased

### Fixed
- Editor login: match the Application API token by the suffixed tenant label
  (`<TenantLabel>Application`), falling back to the bare label for tenants provisioned before the
  convention. Forward-port of the `v2.0.1` hotfix, which landed on `main` against
  `TenantSelectionWindow`; the login refactor had since moved the lookup into
  `EditorSessionManager.FetchTenantTokenAsync`, so the merge would not have carried it.
  The failure log now lists the labels actually received.

## v2.0.0

### Added
- Tenant public config DTO and effective supported-languages helper.
- B2C login flow integrated into the tenant selection window.
- AI API params management.
- `targetPlatform` and `isSelected` fields in `AppConfigurationSettings`.

### Changed
- Refactored the system into a static API layer.
- Merged the login window into the tenant selection window.
- Parallelized `Init` fetches and added `WaitForPublicConfigAsync`.

### Removed
- Removed unused target platform field from tenant selection.

### Fixed
- Fixed editor login state and a silent exception in `TenantSelectionWindow`.

## v1.0.1

### Fixed

- Added checks on `AppConfig` management in `AppConfigurationWindow`.

## v1.0.0

- Initial release
