#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"
darcVersion="1.1.0-beta.19205.4"

while [[ $# > 0 ]]; do
  opt="$(echo "$1" | awk '{print tolower($0)}')"
  case "$opt" in
    --darcversion)
      darcVersion=$2
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

function InstallDarcCli {
  local darc_cli_package_name="microsoft.dotnet.darc"

  InitializeDotNetCli
  local dotnet_root=$_InitializeDotNetCli

  local uninstall_command=`$dotnet_root/dotnet tool uninstall $darc_cli_package_name -g`
  local tool_list=$($dotnet_root/dotnet tool list -g)
  if [[ $tool_list = *$darc_cli_package_name* ]]; then
    echo $($dotnet_root/dotnet tool uninstall $darc_cli_package_name -g)
  fi

  local arcadeServicesSource="https://dotnetfeed.blob.core.windows.net/dotnet-arcade/index.json"

  echo "Installing Darc CLI version $toolset_version..."
  echo "You may need to restart your command shell if this is the first dotnet tool you have installed."
  echo $($dotnet_root/dotnet tool install $darc_cli_package_name --version $darcVersion --add-source "$arcadeServicesSource" -v $verbosity -g)
}

InstallDarcCli
