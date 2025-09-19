#!/usr/bin/env bash

# This file is a temporary workaround for internal builds to be able to restore from private AzDO feeds.
# This file should be removed as part of this issue: https://github.com/dotnet/arcade/issues/4080
#
# What the script does is iterate over all package sources in the pointed NuGet.config and add a credential entry
# under <packageSourceCredentials> for each Maestro's managed private feed. Two additional credential 
# entries are also added for the two private static internal feeds: dotnet3-internal and dotnet3-internal-transport.
#
# This script needs to be called in every job that will restore packages and which the base repo has
# private AzDO feeds in the NuGet.config.
#
# See example YAML call for this script below. Note the use of the variable `$(dn-bot-dnceng-artifact-feeds-rw)`
# from the AzureDevOps-Artifact-Feeds-Pats variable group.
#
# Any disabledPackageSources entries which start with "darc-int" will be re-enabled as part of this script executing.
#
#  - task: Bash@3
#    displayName: Setup Private Feeds Credentials
#    inputs:
#      filePath: $(System.DefaultWorkingDirectory)/eng/common/SetupNugetSources.sh
#      arguments: $(System.DefaultWorkingDirectory)/NuGet.config $Token
#    condition: ne(variables['Agent.OS'], 'Windows_NT')
#    env:
#      Token: $(dn-bot-dnceng-artifact-feeds-rw)

ConfigFile=$1
CredToken=$2
NL='\n'
TB='    '

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

. "$scriptroot/tools.sh"

if [ ! -f "$ConfigFile" ]; then
    Write-PipelineTelemetryError -Category 'Build' "Error: Eng/common/SetupNugetSources.sh returned a non-zero exit code. Couldn't find the NuGet config file: $ConfigFile"
    ExitWithExitCode 1
fi

if [ -z "$CredToken" ]; then
    Write-PipelineTelemetryError -category 'Build' "Error: Eng/common/SetupNugetSources.sh returned a non-zero exit code. Please supply a valid PAT"
    ExitWithExitCode 1
fi

if [[ `uname -s` == "Darwin" ]]; then
    NL=$'\\\n'
    TB=''
fi

# Ensure there is a <packageSources>...</packageSources> section.
grep -i "<packageSources>" $ConfigFile
if [ "$?" != "0" ]; then
    echo "Adding <packageSources>...</packageSources> section."
    ConfigNodeHeader="<configuration>"
    PackageSourcesTemplate="${TB}<packageSources>${NL}${TB}</packageSources>"

    sed -i.bak "s|$ConfigNodeHeader|$ConfigNodeHeader${NL}$PackageSourcesTemplate|" $ConfigFile
fi

# Ensure there is a <packageSourceCredentials>...</packageSourceCredentials> section. 
grep -i "<packageSourceCredentials>" $ConfigFile
if [ "$?" != "0" ]; then
    echo "Adding <packageSourceCredentials>...</packageSourceCredentials> section."

    PackageSourcesNodeFooter="</packageSources>"
    PackageSourceCredentialsTemplate="${TB}<packageSourceCredentials>${NL}${TB}</packageSourceCredentials>"

    sed -i.bak "s|$PackageSourcesNodeFooter|$PackageSourcesNodeFooter${NL}$PackageSourceCredentialsTemplate|" $ConfigFile
fi

PackageSources=()

# Ensure dotnet3.1-internal and dotnet3.1-internal-transport are in the packageSources if the public dotnet3.1 feeds are present
grep -i "<add key=\"dotnet3.1\"" $ConfigFile
if [ "$?" == "0" ]; then
    grep -i "<add key=\"dotnet3.1-internal\"" $ConfigFile
    if [ "$?" != "0" ]; then
        echo "Adding dotnet3.1-internal to the packageSources."
        PackageSourcesNodeFooter="</packageSources>"
        PackageSourceTemplate="${TB}<add key=\"dotnet3.1-internal\" value=\"https://pkgs.dev.azure.com/dnceng/_packaging/dotnet3.1-internal/nuget/v2\" />"

        sed -i.bak "s|$PackageSourcesNodeFooter|$PackageSourceTemplate${NL}$PackageSourcesNodeFooter|" $ConfigFile
    fi
    PackageSources+=('dotnet3.1-internal')

    grep -i "<add key=\"dotnet3.1-internal-transport\">" $ConfigFile
    if [ "$?" != "0" ]; then
        echo "Adding dotnet3.1-internal-transport to the packageSources."
        PackageSourcesNodeFooter="</packageSources>"
        PackageSourceTemplate="${TB}<add key=\"dotnet3.1-internal-transport\" value=\"https://pkgs.dev.azure.com/dnceng/_packaging/dotnet3.1-internal-transport/nuget/v2\" />"

        sed -i.bak "s|$PackageSourcesNodeFooter|$PackageSourceTemplate${NL}$PackageSourcesNodeFooter|" $ConfigFile
    fi
    PackageSources+=('dotnet3.1-internal-transport')
fi

DotNetVersions=('5' '6' '7' '8')

for DotNetVersion in ${DotNetVersions[@]} ; do
    FeedPrefix="dotnet${DotNetVersion}";
    grep -i "<add key=\"$FeedPrefix\"" $ConfigFile
    if [ "$?" == "0" ]; then
        grep -i "<add key=\"$FeedPrefix-internal\"" $ConfigFile
        if [ "$?" != "0" ]; then
            echo "Adding $FeedPrefix-internal to the packageSources."
            PackageSourcesNodeFooter="</packageSources>"
            PackageSourceTemplate="${TB}<add key=\"$FeedPrefix-internal\" value=\"https://pkgs.dev.azure.com/dnceng/internal/_packaging/$FeedPrefix-internal/nuget/v2\" />"

            sed -i.bak "s|$PackageSourcesNodeFooter|$PackageSourceTemplate${NL}$PackageSourcesNodeFooter|" $ConfigFile
        fi
        PackageSources+=("$FeedPrefix-internal")

        grep -i "<add key=\"$FeedPrefix-internal-transport\">" $ConfigFile
        if [ "$?" != "0" ]; then
            echo "Adding $FeedPrefix-internal-transport to the packageSources."
            PackageSourcesNodeFooter="</packageSources>"
            PackageSourceTemplate="${TB}<add key=\"$FeedPrefix-internal-transport\" value=\"https://pkgs.dev.azure.com/dnceng/internal/_packaging/$FeedPrefix-internal-transport/nuget/v2\" />"

            sed -i.bak "s|$PackageSourcesNodeFooter|$PackageSourceTemplate${NL}$PackageSourcesNodeFooter|" $ConfigFile
        fi
        PackageSources+=("$FeedPrefix-internal-transport")
    fi
done

# I want things split line by line
PrevIFS=$IFS
IFS=$'\n'
PackageSources+="$IFS"
PackageSources+=$(grep -oh '"darc-int-[^"]*"' $ConfigFile | tr -d '"')
IFS=$PrevIFS

for FeedName in ${PackageSources[@]} ; do
    # Check if there is no existing credential for this FeedName
    grep -i "<$FeedName>" $ConfigFile 
    if [ "$?" != "0" ]; then
        echo "Adding credentials for $FeedName."

        PackageSourceCredentialsNodeFooter="</packageSourceCredentials>"
        NewCredential="${TB}${TB}<$FeedName>${NL}<add key=\"Username\" value=\"dn-bot\" />${NL}<add key=\"ClearTextPassword\" value=\"$CredToken\" />${NL}</$FeedName>"

        sed -i.bak "s|$PackageSourceCredentialsNodeFooter|$NewCredential${NL}$PackageSourceCredentialsNodeFooter|" $ConfigFile
    fi
done

# Re-enable any entries in disabledPackageSources where the feed name contains darc-int
grep -i "<disabledPackageSources>" $ConfigFile
if [ "$?" == "0" ]; then
    DisabledDarcIntSources=()
    echo "Re-enabling any disabled \"darc-int\" package sources in $ConfigFile"
    DisabledDarcIntSources+=$(grep -oh '"darc-int-[^"]*" value="true"' $ConfigFile  | tr -d '"')
    for DisabledSourceName in ${DisabledDarcIntSources[@]} ; do
        if [[ $DisabledSourceName == darc-int* ]]
            then
                OldDisableValue="<add key=\"$DisabledSourceName\" value=\"true\" />"
                NewDisableValue="<!-- Reenabled for build : $DisabledSourceName -->"
                sed -i.bak "s|$OldDisableValue|$NewDisableValue|" $ConfigFile
                echo "Neutralized disablePackageSources entry for '$DisabledSourceName'"
        fi
    done
fi
