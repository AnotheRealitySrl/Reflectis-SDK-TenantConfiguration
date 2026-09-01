# Release notes

## v2.0.1

### Fixed
- The static API methods now accept a `serverTimeOffset` and `Init` passes the one
  measured by `ApiSystemBase`. They bypass `ApiSystemBase.BuildRequest`, so they used
  to sign the HMAC timestamp with the raw device clock: on a device more than 15s off
  (the server's replay window) the whole tenant-configuration bootstrap returned 401,
  `AppConfig` stayed null and the application blocked access with `MetaverseOffline`.

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
