#requires -version 5
<#
.SYNOPSIS
This script runs a quick check for common errors, such as checking that Visual Studio solutions are up to date or that generated code has been committed to source.
#>
param(
    [switch]$ci,
    # Optional arguments that enable downloading an internal
    # runtime or runtime from a non-default location
    [Alias('DotNetRuntimeSourceFeed')]
    [string]$RuntimeSourceFeed,
    [Alias('DotNetRuntimeSourceFeedKey')]
    [string]$RuntimeSourceFeedKey
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1
Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"

$repoRoot = Resolve-Path "$PSScriptRoot/../.."

[string[]] $errors = @()

function LogError {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$message,
        [string]$FilePath,
        [string]$LineNumber, # Ignored if -FilePath not specified.
        [string]$Code
    )
    if ($env:TF_BUILD) {
        $prefix = "##vso[task.logissue type=error"
        if ($FilePath) {
            $prefix = "${prefix};sourcepath=$FilePath"
            if ($LineNumber) {
                $prefix = "${prefix};linenumber=$LineNumber"
            }
        }
        if ($Code) {
            $prefix = "${prefix};code=$Code"
        }
        Write-Host "${prefix}]${message}"
    }
    $fullMessage = "error ${Code}: $message"
    if ($FilePath) {
        $fullMessage += " [$FilePath"
        if ($LineNumber) {
            $fullMessage += ":$LineNumber"
        }
        $fullMessage += "]"
    }
    Write-Host -f Red $fullMessage
    $script:errors += $fullMessage
}

try {
    if ($ci) {
        # Install dotnet.exe
        if ($RuntimeSourceFeed -or $RuntimeSourceFeedKey) {
            & $repoRoot/restore.cmd -ci -nobl -noBuildNodeJS -RuntimeSourceFeed $RuntimeSourceFeed `
                -RuntimeSourceFeedKey $RuntimeSourceFeedKey
        } else {
            & $repoRoot/restore.cmd -ci -nobl -noBuildNodeJS
        }
    }

    . "$repoRoot/activate.ps1"

    #
    # Duplicate .csproj files can cause issues with a shared build output folder
    #

    $projectFileNames = New-Object 'System.Collections.Generic.HashSet[string]'

    # Ignore duplicates in submodules. These should be isolated from the rest of the build.
    # Ignore duplicates in the .ref folder. This is expected.
    Get-ChildItem -Recurse "$repoRoot/src/*.*proj" |
    Where-Object {
        $_.FullName -NotLike '*\submodules\*' -and $_.FullName -NotLike '*\node_modules\*' -and
        $_.FullName -NotLike '*\bin\*' -and $_.FullName -NotLike '*\src\ProjectTemplates\*\content\*'
    } |
    Where-Object { (Split-Path -Leaf (Split-Path -Parent $_)) -ne 'ref' } |
    ForEach-Object {
        $fileName = [io.path]::GetFileNameWithoutExtension($_)
        if (-not ($projectFileNames.Add($fileName))) {
            LogError -code 'BUILD003' -filepath $_ `
                ("Multiple project files named '$fileName' exist. Project files should have a unique name " +
                 "to avoid conflicts in build output.")
        }
    }

    #
    # Check for unexpected (not from dotnet-public-npm) npm resolutions in lock files.
    #

    $registry = 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public-npm/npm/registry/'
    Get-ChildItem src\package-lock.json -Recurse |
    ForEach-Object FullName |
    Where-Object {$_ -NotLike '*\node_modules\*'} |
    ForEach-Object {
        # -List to limit complaints to one per file.
        Select-String '^  resolved ' $_ | Select-String -List -NotMatch $registry
    } |
    ForEach-Object {
        LogError -filePath "${_.Path}" -lineNumber $_.LineNumber `
            "Packages in package-lock.json file resolved from wrong registry. All dependencies must be resolved from $registry"
    }

    # ComponentsWebAssembly-CSharp.sln is used by the templating engine; MessagePack.sln is irrelevant (in submodule).
    $solution = Get-ChildItem "$repoRoot/AspNetCore.slnx"
    $solutionFile = Split-Path -Leaf $solution

    Write-Host "Checking that $solutionFile is up to date"

    # $solutionProjects will store relative paths i.e. the exact solution and solution filter content.
    $solutionProjects = New-Object 'System.Collections.Generic.HashSet[string]'

    # Where-Object needed to ignore heading `dotnet sln` outputs
    & dotnet sln $solution list  | Where-Object { $_ -like '*proj' } | ForEach-Object {
        $proj = Join-Path $repoRoot $_
        if (-not ($solutionProjects.Add($_))) {
            LogError "Duplicate project. $solutionFile references a project more than once: $proj."
        }
        if (-not (Test-Path $proj)) {
            LogError "Missing project. $solutionFile references a project which does not exist: $proj."
        }
    }

    Write-Host "Checking solution filters"
    Get-ChildItem -Recurse "$repoRoot\*.slnf" | ForEach-Object {
        $solutionFilter = $_
        $json = Get-Content -Raw -Path $solutionFilter |ConvertFrom-Json
        $json.solution.projects | ForEach-Object {
            if (!$solutionProjects.Contains($_)) {
                LogError "$solutionFilter references a project not in $solutionFile`: $_"
            }
        }
    }

    #
    # Generated code check
    #

    Write-Host "Re-running code generation"

    Write-Host "  Re-generating project lists"
    Invoke-Block {
        & $PSScriptRoot\GenerateProjectList.ps1 -ci:$ci
    }

    Write-Host "Running git diff to check for pending changes"

    # Redirect stderr to stdout because PowerShell does not consistently handle output to stderr
    $changedFiles = & cmd /c 'git --no-pager diff --ignore-space-change --name-only 2>nul'

    # Temporary: Disable check for blazor js file and nuget.config (updated automatically for
    # internal builds)
    $changedFilesExclusions = @("src/Components/Web.JS/dist/Release/blazor.server.js", "src/Components/Web.JS/dist/Release/blazor.web.js", "NuGet.config")

    if ($changedFiles) {
        foreach ($file in $changedFiles) {
            if ($changedFilesExclusions -contains $file) {continue}
            $filePath = Resolve-Path "${repoRoot}/${file}"
            LogError  -filepath $filePath `
                ("Generated code is not up to date in $file. You might need to regenerate the reference " +
                 "assemblies or project list (see docs/ReferenceResolution.md)")
            & git --no-pager diff --ignore-space-change $filePath
        }
    }

    $targetBranch = $env:SYSTEM_PULLREQUEST_TARGETBRANCH

    if (![string]::IsNullOrEmpty($targetBranch)) {
        if ($targetBranch.StartsWith('refs/heads/')) {
            $targetBranch = $targetBranch.Replace('refs/heads/','')
        }

        # Retrieve the set of changed files compared to main
        Write-Host "Checking for changes to API baseline files $targetBranch"

        $changedFilesFromTarget = git --no-pager diff origin/$targetBranch --ignore-space-change --name-only --diff-filter=ar
        $changedAPIBaselines = [System.Collections.Generic.List[string]]::new()

        if ($changedFilesFromTarget) {
            foreach ($file in $changedFilesFromTarget) {
                # Check for changes in Shipped in all branches
                if ($file -like '*PublicAPI.Shipped.txt') {
                    if (!$file.Contains('DevServer/src/PublicAPI.Shipped.txt')) {
                        $changedAPIBaselines.Add($file)
                    }
                }
                # Check for changes in Unshipped in servicing branches
                if ($targetBranch -like 'release*' -and $targetBranch -notlike '*preview*' -and $targetBranch -notlike '*rc1*' -and $targetBranch -notlike '*rc2*' -and $file -like '*PublicAPI.Unshipped.txt') {
                    $changedAPIBaselines.Add($file)
                }
            }
        }

        Write-Host "Found changes in $($changedAPIBaselines.count) API baseline files"

        if ($changedAPIBaselines.count -gt 0) {
            LogError ("Detected modification to baseline API files. PublicAPI.Shipped.txt files should only " +
                "be updated after a major release, and PublicAPI.Unshipped.txt files should not " +
                "be updated in release branches. See /docs/APIBaselines.md for more information.")
            LogError "Modified API baseline files:"
            foreach ($file in $changedAPIBaselines) {
                LogError $file
            }
        }

        # Check for relevant changes to SignalR typescript files
        $tsChanges = $changedFilesFromTarget | Where-Object { $_ -like "src/SignalR/clients/ts/*" -and $_ -ne "src/SignalR/clients/ts/CHANGELOG.md" }
        $changelogChanged = $changedFilesFromTarget -contains "src/SignalR/clients/ts/CHANGELOG.md"
        $signalrChangelogOverrideMarker = "[no changelog]"

        # Only enforce changelog rule if there are relevant TS changes
        if ($tsChanges.Count -gt 0 -and -not $changelogChanged) {
            # Check if the override marker exists in recent commit messages
            $hasOverride = git log origin/$targetBranch..HEAD --pretty=%B | Select-String -Pattern $signalrChangelogOverrideMarker -Quiet

            if (-not $hasOverride) {
                LogError "Changes were made to 'src/SignalR/clients/ts/', but no update to 'CHANGELOG.md' was found."
                LogError "Either update 'src/SignalR/clients/ts/CHANGELOG.md' or include '$signalrChangelogOverrideMarker' in your commit message."
                exit 1
            } else {
                Write-Host "SignalR Changelog update skipped due to override marker in commit message."
            }
        }
    }
}
finally {
    Write-Host ""
    Write-Host "Summary:"
    Write-Host ""
    Write-Host "   $($errors.Length) error(s)"
    Write-Host ""

    foreach ($err in $errors) {
        Write-Host -f Red $err
    }

    if ($errors) {
        exit 1
    }
}
