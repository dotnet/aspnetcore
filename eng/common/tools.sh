#!/usr/bin/env bash

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

ci=${ci:-false}
configuration=${configuration:-'Debug'}
nodereuse=${nodereuse:-true}
prepare_machine=${prepare_machine:-false}
restore=${restore:-true}
verbosity=${verbosity:-'minimal'}
warnaserror=${warnaserror:-true}
useInstalledDotNetCli=${useInstalledDotNetCli:-true}

repo_root="$scriptroot/../.."
eng_root="$scriptroot/.."
artifacts_dir="$repo_root/artifacts"
toolset_dir="$artifacts_dir/toolset"
log_dir="$artifacts_dir/log/$configuration"
temp_dir="$artifacts_dir/tmp/$configuration"

global_json_file="$repo_root/global.json"
build_driver=""
toolset_build_proj=""

function ResolvePath {
  local path=$1

  # resolve $path until the file is no longer a symlink
  while [[ -h $path ]]; do
    local dir="$( cd -P "$( dirname "$path" )" && pwd )"
    path="$(readlink "$path")"

    # if $path was a relative symlink, we need to resolve it relative to the path where the
    # symlink file was located
    [[ $path != /* ]] && path="$dir/$path"
  done

  # return value
  _ResolvePath="$path"
}

# ReadVersionFromJson [json key]
function ReadGlobalVersion {
  local key=$1

  local line=`grep -m 1 "$key" "$global_json_file"`
  local pattern="\"$key\" *: *\"(.*)\""

  if [[ ! $line =~ $pattern ]]; then
    echo "Error: Cannot find \"$key\" in $global_json_file" >&2
    ExitWithExitCode 1
  fi

  # return value
  _ReadGlobalVersion=${BASH_REMATCH[1]}
}

function InitializeDotNetCli {
  local install=$1

  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  export DOTNET_MULTILEVEL_LOOKUP=0

  # Disable first run since we want to control all package sources
  export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  # Source Build uses DotNetCoreSdkDir variable
  if [[ -n "${DotNetCoreSdkDir:-}" ]]; then
    export DOTNET_INSTALL_DIR="$DotNetCoreSdkDir"
  fi

  # Find the first path on $PATH that contains the dotnet.exe
  if [[ "$useInstalledDotNetCli" == true && -z "${DOTNET_INSTALL_DIR:-}" ]]; then
    local dotnet_path=`command -v dotnet`
    if [[ -n "$dotnet_path" ]]; then
      ResolvePath "$dotnet_path"
      export DOTNET_INSTALL_DIR=`dirname "$_ResolvePath"`
    fi
  fi

  ReadGlobalVersion "dotnet"
  local dotnet_sdk_version=$_ReadGlobalVersion
  local dotnet_root=""

  # Use dotnet installation specified in DOTNET_INSTALL_DIR if it contains the required SDK version,
  # otherwise install the dotnet CLI and SDK to repo local .dotnet directory to avoid potential permission issues.
  if [[ -n "${DOTNET_INSTALL_DIR:-}" && -d "$DOTNET_INSTALL_DIR/sdk/$dotnet_sdk_version" ]]; then
    dotnet_root="$DOTNET_INSTALL_DIR"
  else
    dotnet_root="$repo_root/.dotnet"
    export DOTNET_INSTALL_DIR="$dotnet_root"

    if [[ ! -d "$DOTNET_INSTALL_DIR/sdk/$dotnet_sdk_version" ]]; then
      if [[ "$install" == true ]]; then
        InstallDotNetSdk "$dotnet_root" "$dotnet_sdk_version"
      else
        echo "Unable to find dotnet with SDK version '$dotnet_sdk_version'" >&2
        ExitWithExitCode 1
      fi
    fi
  fi

  # return value
  _InitializeDotNetCli="$dotnet_root"
}

function InstallDotNetSdk {
  local root=$1
  local version=$2

  GetDotNetInstallScript "$root"
  local install_script=$_GetDotNetInstallScript

  bash "$install_script" --version $version --install-dir "$root"
  local lastexitcode=$?

  if [[ $lastexitcode != 0 ]]; then
    echo "Failed to install dotnet SDK (exit code '$lastexitcode')." >&2
    ExitWithExitCode $lastexitcode
  fi
}

function GetDotNetInstallScript {
  local root=$1
  local install_script="$root/dotnet-install.sh"
  local install_script_url="https://dot.net/v1/dotnet-install.sh"

  if [[ ! -a "$install_script" ]]; then
    mkdir -p "$root"

    echo "Downloading '$install_script_url'"

    # Use curl if available, otherwise use wget
    if command -v curl > /dev/null; then
      curl "$install_script_url" -sSL --retry 10 --create-dirs -o "$install_script"
    else
      wget -q -O "$install_script" "$install_script_url"
    fi
  fi

  # return value
  _GetDotNetInstallScript="$install_script"
}

function InitializeToolset {
  ReadGlobalVersion "Microsoft.DotNet.Arcade.Sdk"

  local toolset_version=$_ReadGlobalVersion
  local toolset_location_file="$toolset_dir/$toolset_version.txt"

  if [[ -a "$toolset_location_file" ]]; then
    local path=`cat "$toolset_location_file"`
    if [[ -a "$path" ]]; then
      toolset_build_proj="$path"
      return
    fi
  fi

  if [[ "$restore" != true ]]; then
    echo "Toolset version $toolsetVersion has not been restored." >&2
    ExitWithExitCode 2
  fi

  local toolset_restore_log="$log_dir/ToolsetRestore.binlog"
  local proj="$toolset_dir/restore.proj"

  echo '<Project Sdk="Microsoft.DotNet.Arcade.Sdk"/>' > "$proj"

  MSBuild "$proj" /t:__WriteToolsetLocation /clp:None /bl:"$toolset_restore_log" /p:__ToolsetLocationOutputFile="$toolset_location_file"
  local lastexitcode=$?

  if [[ $lastexitcode != 0 ]]; then
    echo "Failed to restore toolset (exit code '$lastexitcode'). See log: $toolset_restore_log" >&2
    ExitWithExitCode $lastexitcode
  fi

  toolset_build_proj=`cat "$toolset_location_file"`

  if [[ ! -a "$toolset_build_proj" ]]; then
    echo "Invalid toolset path: $toolset_build_proj" >&2
    ExitWithExitCode 3
  fi
}

function InitializeCustomToolset {
  local script="$eng_root/restore-toolset.sh"

  if [[ -a "$script" ]]; then
    . "$script"
  fi
}

function ConfigureTools {
  local script="$eng_root/configure-toolset.sh"

  if [[ -a "$script" ]]; then
    . "$script"
  fi
}

function InitializeTools {
  ConfigureTools

  InitializeDotNetCli $restore
  build_driver="$_InitializeDotNetCli/dotnet"

  InitializeToolset
  InitializeCustomToolset
}

function ExitWithExitCode {
  if [[ "$ci" == true && "$prepare_machine" == true ]]; then
    StopProcesses
  fi
  exit $1
}

function StopProcesses {
  echo "Killing running build processes..."
  pkill -9 "dotnet"
  pkill -9 "vbcscompiler"
  return 0
}

function MSBuild {
  local warnaserror_switch=""
  if [[ $warnaserror == true ]]; then
    warnaserror_switch="/warnaserror"
  fi

  "$build_driver" msbuild /m /nologo /clp:Summary /v:$verbosity /nr:$nodereuse $warnaserror_switch "$@"

  return $?
}

# HOME may not be defined in some scenarios, but it is required by NuGet
if [[ -z $HOME ]]; then
  export HOME="$repo_root/artifacts/.home/"
  mkdir -p "$HOME"
fi

if [[ -z ${NUGET_PACKAGES:-} ]]; then
  if [[ $ci == true ]]; then
    export NUGET_PACKAGES="$repo_root/.packages"
  else
    export NUGET_PACKAGES="$HOME/.nuget/packages"
  fi
fi

mkdir -p "$toolset_dir"
mkdir -p "$log_dir"

if [[ $ci == true ]]; then
  mkdir -p "$temp_dir"
  export TEMP="$temp_dir"
  export TMP="$temp_dir"
fi
