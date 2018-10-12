#
# README!
# Edits to this script do not automatically change GitHub. You need to get a member of @aspnet/aspnet-org-admins to run this script with their API token (aka personal access token)
#

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    $ApiToken
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

[string[]] $sharedChecks = @('license/cla')

$repos = @(
    @{
        Name   = 'AADIntegration'
        Checks = @('AADIntegration-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'Antiforgery'
        Checks = @('Antiforgery-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'Common'
        Checks = @('Common-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'CORS'
        Checks = @('CORS-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'Diagnostics'
        Checks = @('Diagnostics-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'EventNotification'
        Checks = @('EventNotification-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'HtmlAbstractions'
        Checks = @('HtmlAbstractions-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'IdentityService'
        Checks = @('IdentityService-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'jquery-ajax-unobtrusive'
        Checks = @('jquery-ajax-unobtrusive-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'jquery-validation-unobtrusive'
        Checks = @('continuous-integration/appveyor/pr')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'JsonPatch'
        Checks = @('JsonPatch-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'Localization'
        Checks = @('Localization-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'MusicStore'
        Checks = @('MusicStore-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'Mvc'
        Checks = @('Mvc-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'MvcPrecompilation'
        Checks = @('MvcPrecompilation-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'Razor.LiveShare'
        Checks = @('Razor.LiveShare-ci')
        Branches = @('master')
    },
    @{
        Name   = 'Razor.VSCode'
        Checks = @()
        Branches = @('master')
    },
    @{
        Name   = 'Razor'
        Checks = @('Razor-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'RazorTooling'
        Checks = @('RazorTooling-ci')
        Branches = @('master')
    },
    @{
        Name   = 'Routing'
        Checks = @('Routing-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'Templating'
        Checks = @('Templating-ci')
        Branches = @('master', 'release/2.2')
    },
    @{
        Name   = 'Testing'
        Checks = @('Testing-ci')
        Branches = @('master', 'release/2.2')
    }
)

$headers = @{
    Authorization = "bearer $ApiToken"
}

[string[]] $errors = @()

foreach ($repo in $repos) {
    foreach ($branch in $repo.Branches) {
        
        $repoName = $repo.Name
        [string[]] $contexts = $sharedChecks
        $contexts += $repo.Checks

        $policy = @{
            required_status_checks        = @{
                strict   = $false
                contexts = $contexts
            }
            enforce_admins                = $true
            required_pull_request_reviews = $null
            restrictions                  = $null
        }

        $summary = "Setting checks on ${repoName}:${branch} ($($contexts -join ', '))"
        if ($PSCmdlet.ShouldProcess($summary)) {
            try {
                Invoke-RestMethod -Headers $headers -Method put `
                    -Body (ConvertTo-Json $policy) `
                    "https://api.github.com/repos/aspnet/$repoName/branches/$branch/protection" `
                    -Verbose:$VerbosePreference
            }
            catch {
                $errors += $summary
            }
        }
    }
}

if ($errors) {
    Write-Error "Failed to update:`n$($errors -join "`n")"
    exit 1
}
