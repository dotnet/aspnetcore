#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"
darcVersion=''
versionEndpoint="https://maestro-prod.westus2.cloudapp.azure.com/api/assets/darc-version?api-version=2019-01-16"

while [[ $# > 0 ]]; do
  opt="$(echo "$1" | awk '{print tolower($0)}')"
  case "$opt" in
    --darcversion)
      darcVersion=$2
      shift
      ;;
    --versionendpoint)
      versionEndpoint=$2
      shift
      ;;
    *)
      echo "Invalid argument: $1"
      usage
      exit 1
      ;;
  esac

  shift
done

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
verbosity=m

. "$scriptroot/tools.sh"

if [ -z "$darcVersion" ]; then
  darcVersion=$(curl -X GET "$versionEndpoint" -H "accept: text/plain")
fi

function InstallDarcCli {
  local darc_cli_package_name="microsoft.dotnet.darc"

  InitializeDotNetCli
  local dotnet_root=$_InitializeDotNetCli

  local uninstall_command=`$dotnet_root/dotnet tool uninstall $darc_cli_package_name -g`
  local tool_list=$($dotnet_root/dotnet tool list -g)
  if [[ $tool_list = *$darc_cli_package_name* ]]; then
    echo $($dotnet_root/dotnet tool uninstall $darc_cli_package_name -g)
  fi

  local arcadeServicesSource="https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json"

  echo "Installing Darc CLI version $darcVersion..."
  echo "You may need to restart your command shell if this is the first dotnet tool you have installed."
  echo $($dotnet_root/dotnet tool install $darc_cli_package_name --version $darcVersion --add-source "$arcadeServicesSource" -v $verbosity -g)
}

InstallDarcCli
