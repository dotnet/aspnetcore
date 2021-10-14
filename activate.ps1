#
# This file must be used by invoking ". .\activate.ps1" from the command line.
# You cannot run it directly. See https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_scripts#script-scope-and-dot-sourcing
#
# To exit from the environment this creates, execute the 'deactivate' function.
#

if ($MyInvocation.CommandOrigin -eq 'runspace') {
    Write-Host -f Red "This script cannot be invoked directly."
    Write-Host -f Red "To function correctly, this script file must be 'dot sourced' by calling `". $PSCommandPath`" (notice the dot at the beginning)."
    exit 1
}

function deactivate ([switch]$init) {

    # reset old environment variables
    if (Test-Path variable:_OLD_PATH) {
        $env:PATH = $_OLD_PATH
        Remove-Item variable:_OLD_PATH
    }

    if (test-path function:_old_prompt) {
        Set-Item Function:prompt -Value $function:_old_prompt -ea ignore
        remove-item function:_old_prompt
    }

    Remove-Item env:DOTNET_ROOT -ea ignore
    Remove-Item 'env:DOTNET_ROOT(x86)' -ea Ignore
    Remove-Item env:DOTNET_MULTILEVEL_LOOKUP -ea ignore
    if (-not $init) {
        # Remove the deactivate function
        Remove-Item function:deactivate
    }
}

# Cleanup the environment
deactivate -init

$_OLD_PATH = $env:PATH
# Tell dotnet where to find itself
$env:DOTNET_ROOT = "$PSScriptRoot\.dotnet"
${env:DOTNET_ROOT(x86)} = "$PSScriptRoot\.dotnet\x86"
# Tell dotnet not to look beyond the DOTNET_ROOT folder for more dotnet things
$env:DOTNET_MULTILEVEL_LOOKUP = 0
# Put dotnet first on PATH
$env:PATH = "${env:DOTNET_ROOT};${env:PATH}"

# Set the shell prompt
if (-not $env:DISABLE_CUSTOM_PROMPT) {
    $function:_old_prompt = $function:prompt
    function dotnet_prompt {
        # Add a prefix to the current prompt, but don't discard it.
        write-host "($( split-path $PSScriptRoot -leaf )) " -nonewline
        & $function:_old_prompt
    }

    Set-Item Function:prompt -Value $function:dotnet_prompt -ea ignore
}

Write-Host -f Magenta "Enabled the .NET Core environment. Execute 'deactivate' to exit."
if (-not (Test-Path "${env:DOTNET_ROOT}\dotnet.exe")) {
    Write-Host -f Yellow ".NET Core has not been installed yet. Run $PSScriptRoot\restore.cmd to install it."
}
else {
    Write-Host "dotnet = ${env:DOTNET_ROOT}\dotnet.exe"
}
