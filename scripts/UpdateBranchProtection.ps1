[CmdletBinding(SupportsShouldProcess = $true)]
param(
    $ApiToken
)

$ErrorActionPreference = 'Stop'

$branches = @('master', 'release/2.2')

$repos = @(
    'AADIntegration',
    'Antiforgery',
    'Common',
    'CORS',
    'Diagnostics',
    'EventNotification',
    'HtmlAbstractions',
    'IdentityService',
    'jquery-ajax-unobtrusive',
    'jquery-validation-unobtrusive',
    'JsonPatch',
    'Localization',
    'MusicStore',
    'Mvc',
    'MvcPrecompilation',
    'Razor.LiveShare',
    'Razor.VSCode',
    'Razor',
    'RazorTooling',
    'Routing',
    'Templating',
    'Testing'
)

$headers = @{
    Authorization = "bearer $ApiToken"
}

foreach ($repo in $repos) {
    foreach ($branch in $branches) {
        
        $vstsCIbuild = "$repoName-ci"

        $policy = @{
            required_status_checks = @{
                strict   = $false
                contexts = @(
                    'license/cla', 
                    $vstsCIbuild
                )
            }
            enforce_admins = $true
            required_pull_request_reviews = @{
                require_code_owner_reviews = $true
            }
            restrictions = $null
        }

        $url = "https://api.github.com/repos/aspnet/$repo/branches/$branch/protection"

        if ($PSCmdlet.ShouldProcess($url)) {
            Invoke-RestMethod -Headers $headers -Method put `
            -Body (ConvertTo-Json $policy) `
            $url `
            -Verbose:$VerbosePreference
        }
    }
}