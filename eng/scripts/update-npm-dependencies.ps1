# This script needs to be run on PowerShell 7+ (for ConvertFrom-Json) on Windows (for vsts-npm-auth).
# The GitHub CLI (gh) is required unless `-SkipPullRequestCreation` is passed.

param (
    [switch]$WhatIf,
    [switch]$SkipPullRequestCreation,
    [switch]$SkipClearCache
)

$ErrorActionPreference = "Stop"

$env:npm_config_cache = "$PWD/src/submodules/Node-Externals/cache"

Write-Host "Ensuring the repository is clean"
if (-not $WhatIf) {
    git clean -xdff
}

Write-Host "Removing package-lock.json"
if (-not $WhatIf) {
    Remove-Item .\package-lock.json
}

if (-not $SkipClearCache -and -not $WhatIf) {
    Write-Host "Clearing the npm cache"
    Remove-Item -Recurse -Force "$PWD/src/submodules/Node-Externals/cache"
    New-Item -ItemType Directory -Path "$PWD/src/submodules/Node-Externals/cache"
}

try {
    Get-Command vsts-npm-auth -CommandType ExternalScript
    Write-Host "vsts-npm-auth is already installed"
}
catch {
    Write-Host "Installing vsts-npm-auth"
    if (-not $WhatIf) {
        npm install -g vsts-npm-auth
    }
}

Write-Host "Provisioning a token for the NPM registry. You might be prompted to authenticate."
if (-not $WhatIf) {
    # This command provisions a PAT token for the VSTS NPM registry that lasts for 15 minutes, which is more than enough time to run npm install
    # and ensure any missing package is mirrored.
    vsts-npm-auth -E 15 -F -C .\.npmrc
}

Write-Host "Running npm install"
if (-not $WhatIf) {
    npm install --prefer-online --include optional
}

Write-Host "Adding optional dependencies to the cache"
$rollupOptionalDependencies = (Get-Content .\package-lock.json | ConvertFrom-Json -AsHashtable).packages['node_modules/rollup'].optionalDependencies |
Select-Object '@rollup/rollup-*';
$commonOptionalDependencyVersion = $null;

foreach ($optionalDependency in ($rollupOptionalDependencies | Get-Member -MemberType NoteProperty)) {
    $optionalDependencyName = $optionalDependency.Name
    $optionalDependencyVersion = $rollupOptionalDependencies.$optionalDependencyName

    if ($null -eq $commonOptionalDependencyVersion) {
        $commonOptionalDependencyVersion = $optionalDependencyVersion
    }

    Write-Host "Adding $optionalDependencyName@$optionalDependencyVersion to the cache"
    if (-not $WhatIf) {
        npm cache add $optionalDependencyName@$optionalDependencyVersion
    }
}

if ($null -ne $commonOptionalDependencyVersion) {
    Write-Host "Adding @rollup/wasm-node@$commonOptionalDependencyVersion to the cache"
    if (-not $WhatIf) {
        npm cache add "@rollup/wasm-node@$commonOptionalDependencyVersion"
    }
}

Write-Host "Verifying the cache"
if(-not $WhatIf) {
    npm cache verify
}

# Navigate to the Node-Externals submodule
# Checkout a branch named infrastructure/update-npm-package-cache-<date>
# Stage the changes in the folder
# Commit the changes with the message "Updated npm package cache <date>"
# Use the GH CLI to create a PR for the branch in the origin remote

Push-Location src/submodules/Node-Externals
$branchName = "infrastructure/update-npm-package-cache-$(Get-Date -Format 'yyyy-MM-dd')"
if (-not $WhatIf) {
    git branch -D $branchName 2>$null
    git checkout -b $branchName
    git add .
    git commit -m "Updated npm package cache $(Get-Date -Format 'yyyy-MM-dd')"
}

if ($WhatIf -or $SkipPullRequestCreation) {
    Write-Host "Skipping pull request creation for Node-Externals submodule"
}
else {
    Write-Host "Creating pull request for Node-Externals submodule"
    git branch --set-upstream-to=origin/main
    git push origin $branchName`:$branchName --force;
    gh repo set-default dotnet/Node-Externals
    gh pr create --base main --head "infrastructure/update-npm-package-cache-$(Get-Date -Format 'yyyy-MM-dd')" --title "[Infrastructure] Updated npm package cache $(Get-Date -Format 'yyyy-MM-dd')" --body "Do not merge this PR until the one in aspnetcore passes all checks."
}

## Navigate to the root of the repository
## Checkout a branch named infrastructure/update-npm-package-cache-<date>
## Stage the changes in the folder
## Commit the changes with the message "Updated npm package cache <date>"
## Use the GH CLI to create a PR for the branch in the origin remote
Pop-Location
if(-not $WhatIf) {
    git branch -D $branchName 2>$null
    git checkout -b $branchName
    git add .
    git commit -m "Updated npm package cache $(Get-Date -Format 'yyyy-MM-dd')"
}

if ($WhatIf -or $SkipPullRequestCreation) {
    Write-Host "Skipping pull request creation for the root of the repository"
}
else {
    Write-Host "Creating pull request for the root of the repository"
    git branch --set-upstream-to=origin/main
    git push origin $branchName`:$branchName --force;
    gh repo set-default dotnet/aspnetcore
    gh pr create --base main --head $branchName --title "[Infrastructure] Updated npm package cache $(Get-Date -Format 'yyyy-MM-dd')" --body ""
}
