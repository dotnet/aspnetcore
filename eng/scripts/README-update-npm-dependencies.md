# Updating NPM Dependencies

This document describes how to update NPM dependencies in the ASP.NET Core repository.

## Prerequisites

- **Windows OS**: The `update-npm-dependencies.ps1` script requires Windows because it uses `vsts-npm-auth` for Azure DevOps NPM registry authentication
- **PowerShell 7+**: Required for `ConvertFrom-Json` functionality
- **GitHub CLI (gh)**: Required only if you want the script to automatically create a PR. Not needed if using `-SkipPullRequestCreation`
- **vsts-npm-auth**: Installed automatically by the script if not present

## Running the Script

### Option 1: Automatic PR Creation (Default)

Run the script without parameters to have it automatically create a pull request:

```powershell
.\eng\scripts\update-npm-dependencies.ps1
```

This will:
1. Clean the repository
2. Remove and regenerate `package-lock.json`
3. Update npm packages
4. Create a new branch named `infrastructure/update-npm-packages-<date>`
5. Commit the changes
6. Push the branch and create a PR using GitHub CLI

### Option 2: Manual PR Creation

To run the script and manually create the PR yourself:

```powershell
.\eng\scripts\update-npm-dependencies.ps1 -SkipPullRequestCreation
```

This will:
1. Clean the repository
2. Remove and regenerate `package-lock.json`
3. Update npm packages
4. Create a new branch named `infrastructure/update-npm-packages-<date>`
5. Commit the changes locally
6. **Skip** pushing the branch and creating a PR

After running with `-SkipPullRequestCreation`, you can manually:
- Review the changes: `git diff origin/main`
- Push the branch: `git push origin <branch-name>`
- Create a PR through the GitHub web interface or using `gh pr create`

### Option 3: Dry Run

To see what the script would do without making any changes:

```powershell
.\eng\scripts\update-npm-dependencies.ps1 -WhatIf
```

## Parameters

- `-WhatIf`: Performs a dry run without making any changes
- `-SkipPullRequestCreation`: Skips automatic PR creation, allowing you to create the PR manually

## Authentication

The script will prompt you to authenticate with Azure DevOps when running `vsts-npm-auth`. This provisions a temporary PAT token for accessing the internal NPM registry.

## What the Script Does

1. **Cleans the repository**: Runs `git clean -xdff` to remove all untracked files
2. **Removes package-lock.json**: Deletes the existing lock file to force regeneration
3. **Authenticates**: Uses `vsts-npm-auth` to authenticate with the Azure DevOps NPM registry
4. **Installs packages**: Runs `npm install --prefer-online --include optional`
5. **Caches optional dependencies**: Adds rollup optional dependencies to the npm cache
6. **Creates a branch**: Creates a new branch named `infrastructure/update-npm-packages-<date>`
7. **Commits changes**: Commits all changes with message "Updated npm packages <date>"
8. **Creates PR** (optional): If not using `-SkipPullRequestCreation`, pushes the branch and creates a PR

## Troubleshooting

### Authentication Errors

If you encounter authentication errors with `vsts-npm-auth`:
- Ensure you're on Windows
- Ensure you're logged into Azure DevOps
- Try running `vsts-npm-auth -F` manually to test authentication

### The script cleaned my repository

The script runs `git clean -xdff` which removes all untracked files. Make sure you've committed or stashed any work in progress before running the script.

### Package installation fails

Ensure you have access to the Azure DevOps NPM registry at `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public-npm/npm/registry/`.
