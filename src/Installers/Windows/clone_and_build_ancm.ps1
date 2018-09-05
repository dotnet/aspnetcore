#
# This builds installers for AspNetCoreModule.
# This script requires internal-only access to the code which generates ANCM installers.
#
#requires -version 4
[cmdletbinding()]
param(
    [string]$GitCredential,
    [string]$Configuration = 'Release',
    [string]$DepsZip,
    [string]$BuildNumber = 't000',
    [string]$AncmSourceBranch = 'release/2.2',
    [string]$SignType = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot/../../../"

Import-Module -Scope Local "$repoRoot/scripts/common.psm1" -Force

$msbuild = Get-MSBuildPath -Prerelease -requires 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64'

# get wix
[version] $wixVer = '3.11.1'
$wixToolSetRoot = "$repoRoot/obj/tools/wix/$wixVer/"
$downloadFile = "$wixToolSetRoot/wix-binaries.zip"
if (-not (Test-Path $downloadFile)) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $downloadUrl = "https://github.com/wixtoolset/wix3/releases/download/wix$($wixVer.Major)$($wixVer.Minor)$($wixVer.Build)rtm/wix$($wixVer.Major)$($wixVer.Minor)-binaries.zip"
    Write-Host "Downloading fix $wixVer from $downloadUrl"
    New-Item -Type Directory $wixToolSetRoot -ErrorAction Ignore | Out-Null
    Invoke-WebRequest -UseBasicParsing -Uri $downloadUrl -OutFile $downloadFile
    Expand-Archive $downloadFile -DestinationPath $wixToolSetRoot
}

# get nuget.exe
$nuget = "$repoRoot/obj/tools/nuget.exe"
if (-not (Test-Path $nuget)) {
    Invoke-WebRequest -UseBasicParsing -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nuget
}

if (-not $DepsZip) {
    $DepsZip = "$repoRoot/modules/IISIntegration/artifacts/build/AspNetCoreModule.zip"
}

$deps = "$repoRoot/obj/ancm"
Remove-Item -Recurse $deps -Force -ErrorAction Ignore | Out-Null
New-Item -ItemType Directory -ErrorAction Ignore $deps | Out-Null
Expand-Archive $DepsZip -DestinationPath $deps -Force

Push-Location $PSScriptRoot

try {
    if ($GitCredential) {
        # Disable prompts for passwords
        $env:GIT_TERMINAL_PROMPT = 0
    }

    if (-not (Test-Path ancm/)) {
        $gitSource = "https://${GitCredential}@devdiv.visualstudio.com/DefaultCollection/DevDiv/_git"
        Invoke-Block {
            & git clone $gitSource/AspNetCoreModule-Setup `
                --branch $AncmSourceBranch `
                ancm/
        }
        Invoke-Block {
            & git clone $gitSource/IIS-Setup `
                ancm/IIS-Setup
        }
        Invoke-Block {
            & git clone $gitSource/IIS-Common `
                ancm/IIS-Common
        }
        Invoke-Block {
            & git clone $gitSource/IIS-Common `
                ancm/IIS-Setup/IIS-Common
        }
    }

    Invoke-Block -WorkingDir ancm/ {
        & git submodule update --init --recursive
    }

    Invoke-Block -WorkingDir ancm/ {
        & $nuget restore ANCM-Setup.sln
    }

    Invoke-Block -WorkingDir ancm/IIS-Common/lib/ {
        & $nuget restore packages.config
    }

    Invoke-Block { & $msbuild `
            ancm/Setup.msbuild `
            -m `
            -v:m `
            -nodeReuse:false `
            -clp:Summary `
            "-t:BuildCustomAction;Build" `
            "-p:PrebuiltAspnetCoreRoot=$deps" `
            "-p:WixToolPath=$wixToolSetRoot" `
            "-p:WixTargetsPath=$wixToolSetRoot\Wix.targets" `
            "-p:WixTasksPath=$wixToolSetRoot\wixtasks.dll" `
            "-p:WixNativeCATargetsPath=$wixToolSetRoot\sdk\wix.nativeca.targets" `
            "-p:Configuration=$Configuration" `
            "-p:BuildNumber=$BuildNumber" `
            "-p:SignType=$SignType" `
            "-bl:$repoRoot/artifacts/logs/ancn.msbuild.binlog"
    }

    $outputPath = "$repoRoot/artifacts/bin/$Configuration/installers/en-US/"
    New-Item $outputPath -ItemType Directory -ErrorAction Ignore | Out-Null
    Copy-Item -Recurse "ancm/bin/AspNetCoreModuleV1/$Configuration/x64/en-us/*" $outputPath
    Copy-Item -Recurse "ancm/bin/AspNetCoreModuleV1/$Configuration/x86/en-us/*" $outputPath
    Copy-Item -Recurse "ancm/bin/AspNetCoreModuleV2/$Configuration/x64/en-us/*" $outputPath
    Copy-Item -Recurse "ancm/bin/AspNetCoreModuleV2/$Configuration/x86/en-us/*" $outputPath
}
finally {
    Pop-Location
}
