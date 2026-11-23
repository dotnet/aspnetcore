# Mints a short-lived GitHub App installation access token by signing a JWT
# via Azure Key Vault's RSA signing operation (RS256). The private key never
# leaves Key Vault. The JWT is exchanged for a single installation's token.
#
# Requirements:
#   - The GitHub App's private key imported as a Key Vault RSA key.
#   - Read access to the configured App-ID secret projection.
#   - The federated Azure service connection must have the `Sign` action on
#     that key. Reading private-key secrets is neither required nor supported.
#   - The App must be installed on the target organization/account
#     (`InstallationOwner`) with the permissions/repositories it needs.
#
# Installation tokens (ghs_*) are exempt from the enterprise classic-PAT
# lifetime policy, which is why this replaces the long-lived PAT.

[CmdletBinding()]
param(
    # Key Vault holding the App-ID projection and RSA signing key.
    [Parameter(Mandatory = $true)]
    [string] $KeyVaultName,

    # Name of the RSA signing key, not a Key Vault secret.
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $KeyName,

    # Secret Manager projection containing the GitHub App's public ID.
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $AppIdSecretName,

    # Login of the organization or user account whose installation we should
    # mint the token for (e.g. `dotnet`, `microsoft`).
    [Parameter(Mandatory = $true)]
    [string] $InstallationOwner,

    # Optional Azure DevOps pipeline variable name to set with the installation
    # token (marked as a secret). When not specified, the token is written to
    # stdout instead.
    [Parameter(Mandatory = $false)]
    [string] $OutputVariableName
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

. $PSScriptRoot\pipeline-logging-functions.ps1

if ($KeyVaultName -notmatch '^[A-Za-z][A-Za-z0-9-]{1,22}[A-Za-z0-9]$' -or $KeyVaultName.Contains('--')) {
    Write-PipelineTelemetryError -Category 'Build' -Message "KeyVaultName '$KeyVaultName' is not a valid Azure Key Vault name."
    exit 1
}

function ConvertTo-Base64Url([byte[]] $bytes) {
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

Write-Host "Reading GitHub App ID from secret '$AppIdSecretName' in vault '$KeyVaultName'..."
$previousNativeCommandErrorPreference = $PSNativeCommandUseErrorActionPreference
try {
    $PSNativeCommandUseErrorActionPreference = $false
    # az keyvault secret show fails for this projection with Errno 22; az rest keeps authentication in Azure CLI.
    $escapedSecretName = [Uri]::EscapeDataString($AppIdSecretName)
    $appId = az rest `
        --method get `
        --url "https://$KeyVaultName.vault.azure.net/secrets/${escapedSecretName}?api-version=7.4" `
        --resource https://vault.azure.net `
        --query value `
        --output tsv `
        --only-show-errors
    $secretExitCode = $LASTEXITCODE
}
catch {
    Write-PipelineTelemetryError -Category 'Build' -Message "Failed to read App ID secret '$AppIdSecretName' from vault '$KeyVaultName' with Azure CLI: $_"
    exit 1
}
finally {
    $PSNativeCommandUseErrorActionPreference = $previousNativeCommandErrorPreference
}
if ($secretExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($appId)) {
    Write-PipelineTelemetryError -Category 'Build' -Message "'az rest' failed with exit code $secretExitCode while reading App ID secret '$AppIdSecretName' from vault '$KeyVaultName'. Verify the service connection can read this App-ID projection."
    exit 1
}

# Build JWT header and payload. Use [ordered] hashtables so JSON
# serialization is deterministic.
$jwtHeader = [ordered]@{
    alg = 'RS256'
    typ = 'JWT'
}
$now = [System.DateTimeOffset]::UtcNow
$jwtPayload = [ordered]@{
    iat = $now.AddMinutes(-1).ToUnixTimeSeconds()
    exp = $now.AddMinutes(5).ToUnixTimeSeconds()
    iss = $appId
}

$headerEncoded  = ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes(($jwtHeader  | ConvertTo-Json -Compress)))
$payloadEncoded = ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes(($jwtPayload | ConvertTo-Json -Compress)))
$signingInput   = "$headerEncoded.$payloadEncoded"

$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $digestBytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signingInput))
}
finally {
    $sha256.Dispose()
}

$digestBase64 = [Convert]::ToBase64String($digestBytes)
Write-Host "Signing JWT with key '$KeyName' in vault '$KeyVaultName'..."
$previousNativeCommandErrorPreference = $PSNativeCommandUseErrorActionPreference
try {
    # Azure CLI can emit non-fatal Python warnings to stderr.
    $PSNativeCommandUseErrorActionPreference = $false
    $signatureBase64 = az keyvault key sign `
        --vault-name $KeyVaultName `
        --name $KeyName `
        --algorithm RS256 `
        --digest $digestBase64 `
        --query signature `
        --output tsv `
        --only-show-errors
    $signExitCode = $LASTEXITCODE
}
catch {
    Write-PipelineTelemetryError -Category 'Build' -Message "Failed to sign the JWT via Key Vault (key '$KeyName', vault '$KeyVaultName'): $_. Verify the service connection has the Sign action on the key."
    exit 1
}
finally {
    $PSNativeCommandUseErrorActionPreference = $previousNativeCommandErrorPreference
}
if ($signExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($signatureBase64)) {
    Write-PipelineTelemetryError -Category 'Build' -Message "'az keyvault key sign' exited with code $signExitCode for key '$KeyName' in vault '$KeyVaultName'. Verify the service connection has the Sign action on the key."
    exit 1
}
$signatureUrl = $signatureBase64.Trim().TrimEnd('=').Replace('+', '-').Replace('/', '_')
$jwt = "$signingInput.$signatureUrl"

$headers = @{
    Authorization          = "Bearer $jwt"
    'X-GitHub-Api-Version' = '2022-11-28'
    Accept                 = 'application/vnd.github+json'
    'User-Agent'           = 'dotnet-arcade-onelocbuild'
}

Write-Host "Looking up installation for '$InstallationOwner'..."
try {
    $installations = [System.Collections.Generic.List[object]]::new()
    $page = 1
    do {
        $pageResponse = Invoke-RestMethod `
            -Uri "https://api.github.com/app/installations?per_page=100&page=$page" `
            -Headers $headers `
            -Method Get
        $pageInstallationCount = 0
        foreach ($installation in $pageResponse) {
            $installations.Add($installation)
            $pageInstallationCount++
        }
        $page++
    } while ($pageInstallationCount -eq 100)
}
catch {
    Write-PipelineTelemetryError -Category 'Build' -Message "Failed to list GitHub App installations: $_. The signed JWT may be invalid or the App ID may be incorrect."
    exit 1
}
$matchingInstallations = @($installations | Where-Object { $_.account.login -ieq $InstallationOwner })
if ($matchingInstallations.Count -eq 0) {
    $found = ($installations | ForEach-Object { $_.account.login }) -join ', '
    Write-PipelineTelemetryError -Category 'Build' -Message "No installation found for '$InstallationOwner'. App is installed on: $found"
    exit 1
}
if ($matchingInstallations.Count -ne 1) {
    $matchingIds = ($matchingInstallations | ForEach-Object { $_.id }) -join ', '
    Write-PipelineTelemetryError -Category 'Build' -Message "Found multiple installations for '$InstallationOwner': $matchingIds"
    exit 1
}
$installation = $matchingInstallations[0]
Write-Host "Using installation $($installation.id) for '$($installation.account.login)'."

try {
    $tokenResponse = Invoke-RestMethod `
        -Uri "https://api.github.com/app/installations/$($installation.id)/access_tokens" `
        -Headers $headers `
        -Method Post `
        -ContentType 'application/json'
}
catch {
    Write-PipelineTelemetryError -Category 'Build' -Message "Failed to mint an installation access token for '$InstallationOwner' (installation $($installation.id)): $_"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($tokenResponse.token)) {
    Write-PipelineTelemetryError -Category 'Build' -Message "GitHub returned an empty installation token for '$InstallationOwner'."
    exit 1
}

Write-Host "Got installation token for '$InstallationOwner' (expires $($tokenResponse.expires_at))."
if ($OutputVariableName) {
    Write-Host "Setting pipeline variable '$OutputVariableName'."
    Write-Host "##vso[task.setvariable variable=$OutputVariableName;issecret=true]$($tokenResponse.token)"
}
else {
    Write-Host $tokenResponse.token -ForegroundColor Green
}
