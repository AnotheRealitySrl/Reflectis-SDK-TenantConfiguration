# Release notes

## v2.0.1

### Fixed
- Editor login: the Application API token is now matched by the `<TenantLabel>Application` label, falling back to the bare tenant label for tenants provisioned before that convention. Same rule the WebGL browser bridge already applies; without it the login failed with "No token found for API label: `<tenant>`" on every tenant using the current convention.
- Editor login: the failure log lists the token labels actually received, which separates "no token was minted" from "minted under a label we don't match".

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
