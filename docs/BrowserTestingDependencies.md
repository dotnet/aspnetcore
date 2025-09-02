# Browser Testing Dependencies

This document describes the browser testing dependencies used in ASP.NET Core and how to keep them updated.

## Current Dependencies

### Selenium WebDriver
- **Packages**: `Selenium.WebDriver`, `Selenium.Support`
- **Current Version**: 4.34.0
- **Configuration**: Defined in `eng/Versions.props` as `SeleniumWebDriverVersion` and `SeleniumSupportVersion`
- **Usage**: Used for E2E testing in Components tests (`src/Components/test/E2ETest`)

### Microsoft Playwright
- **Package**: `Microsoft.Playwright`
- **Current Version**: 1.54.0
- **Configuration**: Defined in `eng/Versions.props` as `MicrosoftPlaywrightVersion`
- **Docker Image**: `mcr.microsoft.com/playwright/dotnet:v1.54.0-jammy-amd64`
- **Docker File**: `src/Components/benchmarkapps/Wasm.Performance/dockerfile`
- **Usage**: Used for benchmarking applications

## Update Process

### Automated Update Script
A PowerShell script exists at `eng/scripts/update-selenium-and-playwright-versions.ps1` that:
1. Checks for the latest versions from NuGet.org
2. Updates `eng/Versions.props` with new versions
3. Updates the Playwright Docker image in the dockerfile
4. Creates a branch and commit with the changes

### Manual Update Process
1. Check for latest versions available in the configured NuGet feeds (not NuGet.org directly)
2. Update versions in `eng/Versions.props`:
   - `<SeleniumWebDriverVersion>X.X.X</SeleniumWebDriverVersion>`
   - `<SeleniumSupportVersion>X.X.X</SeleniumSupportVersion>`
   - `<MicrosoftPlaywrightVersion>X.X.X</MicrosoftPlaywrightVersion>`
3. Update Docker image version in `src/Components/benchmarkapps/Wasm.Performance/dockerfile`:
   - `FROM mcr.microsoft.com/playwright/dotnet:vX.X.X-jammy-amd64 AS final`
4. Verify that the Docker image version exists at https://mcr.microsoft.com/v2/playwright/dotnet/tags/list
5. Test the build: `./restore.sh`

### Version Alignment Requirements
- Playwright NuGet package version must match the Docker image version
- Both Selenium packages should use the same version number
- Only use stable (non-prerelease) versions

### NuGet Feed Considerations
The repository uses internal Microsoft NuGet feeds rather than NuGet.org directly:
- `dotnet-public` - Primary feed for stable packages
- Other internal feeds for specific .NET versions

This means that the latest versions on NuGet.org might not be immediately available in the internal feeds.

## Verification

To verify current dependency status:

```bash
# Check current versions
grep -E "(SeleniumWebDriverVersion|SeleniumSupportVersion|MicrosoftPlaywrightVersion)" eng/Versions.props

# Check Playwright Docker image
grep "playwright/dotnet:" src/Components/benchmarkapps/Wasm.Performance/dockerfile

# Test build
./restore.sh
```

## Last Updated
- **Date**: September 2, 2025
- **Selenium Version**: 4.34.0 (latest available in internal feeds)
- **Playwright Version**: 1.54.0 (aligned with Docker image)
- **Status**: ✅ All dependencies current and aligned