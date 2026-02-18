# Azure DevOps and Helix Reference

## Supported Repositories

The script works with any dotnet repository that uses Azure DevOps and Helix:

| Repository | Common Pipelines |
|------------|-----------------|
| `dotnet/runtime` | runtime, runtime-dev-innerloop, dotnet-linker-tests |
| `dotnet/sdk` | dotnet-sdk (mix of local and Helix tests) |
| `dotnet/aspnetcore` | aspnetcore-ci |
| `dotnet/roslyn` | roslyn-CI |
| `dotnet/maui` | maui-public |

Use `-Repository` to specify the target:
```powershell
./scripts/Get-CIStatus.ps1 -PRNumber 12345 -Repository "dotnet/aspnetcore"
```

## Build Definition IDs (Example: dotnet/aspnetcore)

Each repository has its own build definition IDs. Here are common ones for dotnet/aspnetcore:

| Definition ID | Name | Description |
|---------------|------|-------------|
| `83` | aspnetcore-ci | Main PR validation build |
| `86` | aspnetcore-quarantined-pr | Flaky tests quarantined into their own pipeline |
| `87` | aspnetcore-components-e2e | Components end-to-end tests |
| `318` | aspnetcore-template-tests-pr | Template tests |

**Note:** The script auto-discovers builds for a PR, so you rarely need to know definition IDs.

## Azure DevOps Organizations

**Public builds (default):**
- Organization: `dnceng-public`
- Project: `cbb18261-c48f-4abb-8651-8cdcb5474649`

**Internal/private builds:**
- Organization: `dnceng`
- Project GUID: Varies by pipeline

Override with:
```powershell
./scripts/Get-CIStatus.ps1 -BuildId 1276327 -Organization "dnceng" -Project "internal-project-guid"
```

## Common Pipeline Names (Example: dotnet/aspnetcore)

| Pipeline | Description |
|----------|-------------|
| `aspnetcore-ci` | Main PR validation build |
| `aspnetcore-quarantined-pr` | Flaky tests quarantined into their own pipeline |
| `aspnetcore-components-e2e` | Components end-to-end tests |
| `aspnetcore-template-tests-pr` | Template tests |

Other repos have different pipelines - the script discovers them automatically from the PR.

## Useful Links

- [Helix Portal](https://helix.dot.net/): View Helix jobs and work items (all repos)
- [Helix API Documentation](https://helix.dot.net/swagger/): Swagger docs for Helix REST API
- [Build Analysis](https://github.com/dotnet/arcade/blob/main/Documentation/Projects/Build%20Analysis/LandingPage.md): Known issues tracking (arcade infrastructure)
- [dnceng-public AzDO](https://dev.azure.com/dnceng-public/public/_build): Public builds for all dotnet repos

### Repository-specific docs:
- [aspnetcore: Area Owners](https://github.com/dotnet/aspnetcore/blob/main/docs/area-owners.md)

## Test Execution Types

### Helix Tests
Tests run on Helix distributed test infrastructure. The script extracts console log URLs and can fetch detailed failure info with `-ShowLogs`.

### Local Tests (Non-Helix)
Some repositories (e.g., dotnet/sdk) run tests directly on the build agent. The script detects these and extracts Azure DevOps Test Run URLs.

## Known Issue Labels

- `Known Build Error` - Used by Build Analysis across all dotnet repositories
- Search syntax: `repo:<owner>/<repo> is:issue is:open label:"Known Build Error" <test-name>`

Example searches (use `search_issues` when GitHub MCP is available, `gh` CLI otherwise):
```bash
# Search in runtime
gh issue list --repo dotnet/runtime --label "Known Build Error" --search "FileSystemWatcher"

# Search in aspnetcore
gh issue list --repo dotnet/aspnetcore --label "Known Build Error" --search "Blazor"

# Search in sdk
gh issue list --repo dotnet/sdk --label "Known Build Error" --search "template"
```
