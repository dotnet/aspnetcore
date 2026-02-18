# Deep Investigation with Azure CLI

The AzDO MCP tools handle most pipeline queries directly. This reference covers the Azure CLI fallback for cases where MCP tools are unavailable or the endpoint isn't exposed (e.g., downloading artifacts, inspecting pipeline definitions).

When the CI script and GitHub APIs aren't enough (e.g., investigating internal pipeline definitions or downloading build artifacts), use the Azure CLI with the `azure-devops` extension.

> 💡 **Prefer `az pipelines` / `az devops` commands over raw REST API calls.** The CLI handles authentication, pagination, and JSON output formatting. Only fall back to manual `Invoke-RestMethod` calls when the CLI doesn't expose the endpoint you need (e.g., build timelines). The CLI's `--query` (JMESPath) and `-o table` flags are powerful for filtering without extra scripting.

## Checking Authentication

Before making AzDO API calls, verify the CLI is installed and authenticated:

```powershell
# Ensure az is on PATH (Windows may need a refresh after install)
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

# Check if az CLI is available
az --version 2>$null | Select-Object -First 1

# Check if logged in and get current account
az account show --query "{name:name, user:user.name}" -o table 2>$null

# If not logged in, prompt the user to authenticate:
#   az login                              # Interactive browser login
#   az login --use-device-code            # Device code flow (for remote/headless)

# Get an AAD access token for AzDO REST API calls (only needed for raw REST)
$accessToken = (az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)
$headers = @{ "Authorization" = "Bearer $accessToken" }
```

> ⚠️ If `az` is not installed, use `winget install -e --id Microsoft.AzureCLI` (Windows). The `azure-devops` extension is also required — install or verify it with `az extension add --name azure-devops` (safe to run if already installed). Ask the user to authenticate if needed.

> ⚠️ **Do NOT use `az devops configure --defaults`** — it sets user-wide defaults that may not match the organization/project needed for dotnet repositories. Always pass `--org` and `--project` (or `-p`) explicitly on each command.

## Querying Pipeline Definitions and Builds

```powershell
$org = "https://dev.azure.com/dnceng"
$project = "internal"

# Find a pipeline definition by name
az pipelines list --name "dotnet-unified-build" --org $org -p $project --query "[].{id:id, name:name, path:path}" -o table

# Get pipeline definition details (shows YAML path, triggers, etc.)
az pipelines show --id 1330 --org $org -p $project --query "{id:id, name:name, yamlPath:process.yamlFilename, repo:repository.name}" -o table

# List recent builds for a pipeline (replace {TARGET_BRANCH} with the PR's base branch, e.g., main or release/9.0)
az pipelines runs list --pipeline-ids 1330 --branch "refs/heads/{TARGET_BRANCH}" --top 5 --org $org -p $project --query "[].{id:id, result:result, finish:finishTime}" -o table

# Get a specific build's details
az pipelines runs show --id $buildId --org $org -p $project --query "{id:id, result:result, sourceBranch:sourceBranch}" -o table

# List build artifacts
az pipelines runs artifact list --run-id $buildId --org $org -p $project --query "[].{name:name, type:resource.type}" -o table

# Download a build artifact
az pipelines runs artifact download --run-id $buildId --artifact-name "TestBuild_linux_x64" --path "$env:TEMP\artifact" --org $org -p $project
```

## REST API Fallback

Fall back to REST API only when the CLI doesn't expose what you need:

```powershell
# Get build timeline (stages, jobs, tasks with results and durations) — no CLI equivalent
$accessToken = (az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)
$headers = @{ "Authorization" = "Bearer $accessToken" }
$timelineUrl = "https://dev.azure.com/dnceng/internal/_apis/build/builds/$buildId/timeline?api-version=7.1"
$timeline = (Invoke-RestMethod -Uri $timelineUrl -Headers $headers)
$timeline.records | Where-Object { $_.result -eq "failed" -and $_.type -eq "Job" }
```

## Examining Pipeline YAML

All dotnet repos that use arcade put their pipeline definitions under `eng/pipelines/`. Use `az pipelines show` to find the YAML file path, then fetch it:

```powershell
# Find the YAML path for a pipeline
az pipelines show --id 1330 --org $org -p $project --query "{yamlPath:process.yamlFilename, repo:repository.name}" -o table

# Fetch the YAML from the repo (example: dotnet/runtime's runtime-official pipeline)
#   Read the pipeline YAML from the repo to understand build stages and conditions
#   e.g., eng/pipelines/runtime-official.yml in dotnet/runtime

# For VMR unified builds, the YAML is in dotnet/dotnet:
#   eng/pipelines/unified-build.yml

# Templates are usually in eng/pipelines/common/ or eng/pipelines/templates/
```

This is especially useful when:
- A job name doesn't clearly indicate what it builds
- You need to understand stage dependencies (why a job was canceled)
- You want to find which template defines a specific step
- Investigating whether a pipeline change caused new failures
