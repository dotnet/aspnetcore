#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

. $scriptroot/common-library.sh

base_uri=
install_path=
version=
clean=false
force=false
download_retries=5
retry_wait_time_seconds=30

while (($# > 0)); do
  lowerI="$(echo $1 | awk '{print tolower($0)}')"
  case $lowerI in
    --baseuri)
      base_uri=$2
      shift 2
      ;;
    --installpath)
      install_path=$2
      shift 2
      ;;
    --version)
      version=$2
      shift 2
      ;;
    --clean)
      clean=true
      shift 1
      ;;
    --force)
      force=true
      shift 1
      ;;
    --downloadretries)
      download_retries=$2
      shift 2
      ;;
    --retrywaittimeseconds)
      retry_wait_time_seconds=$2
      shift 2
      ;;
    --help)
      echo "Common settings:"
      echo "  --baseuri <value>        Base file directory or Url wrom which to acquire tool archives"
      echo "  --installpath <value>    Base directory to install native tool to"
      echo "  --clean                  Don't install the tool, just clean up the current install of the tool"
      echo "  --force                  Force install of tools even if they previously exist"
      echo "  --help                   Print help and exit"
      echo ""
      echo "Advanced settings:"
      echo "  --downloadretries        Total number of retry attempts"
      echo "  --retrywaittimeseconds   Wait time between retry attempts in seconds"
      echo ""
      exit 0
      ;;
  esac
done

tool_name="cmake"
tool_os=$(GetCurrentOS)
tool_folder=$(echo $tool_os | awk '{print tolower($0)}')
tool_arch="x86_64"
tool_name_moniker="$tool_name-$version-$tool_os-$tool_arch"
tool_install_directory="$install_path/$tool_name/$version"
tool_file_path="$tool_install_directory/$tool_name_moniker/bin/$tool_name"
shim_path="$install_path/$tool_name.sh"
uri="${base_uri}/$tool_folder/cmake/$tool_name_moniker.tar.gz"

# Clean up tool and installers
if [[ $clean = true ]]; then
  echo "Cleaning $tool_install_directory"
  if [[ -d $tool_install_directory ]]; then
    rm -rf $tool_install_directory
  fi

  echo "Cleaning $shim_path"
  if [[ -f $shim_path ]]; then
    rm -rf $shim_path
  fi

  tool_temp_path=$(GetTempPathFileName $uri)
  echo "Cleaning $tool_temp_path"
  if [[ -f $tool_temp_path ]]; then
    rm -rf $tool_temp_path
  fi

  exit 0
fi

# Install tool
if [[ -f $tool_file_path ]] && [[ $force = false ]]; then
  echo "$tool_name ($version) already exists, skipping install"
  exit 0
fi

DownloadAndExtract $uri $tool_install_directory $force $download_retries $retry_wait_time_seconds

if [[ $? != 0 ]]; then
  echo "Installation failed" >&2
  exit 1
fi

# Generate Shim
# Always rewrite shims so that we are referencing the expected version
NewScriptShim $shim_path $tool_file_path true

if [[ $? != 0 ]]; then
  echo "Shim generation failed" >&2
  exit 1
fi

exit 0