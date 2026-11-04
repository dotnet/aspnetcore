#!/usr/bin/env bash

function GetNativeInstallDirectory {
  local install_dir

  if [[ -z $NETCOREENG_INSTALL_DIRECTORY ]]; then
    install_dir=$HOME/.netcoreeng/native/
  else
    install_dir=$NETCOREENG_INSTALL_DIRECTORY
  fi

  echo $install_dir
  return 0
}

function GetTempDirectory {

  echo $(GetNativeInstallDirectory)temp/
  return 0
}

function ExpandZip {
  local zip_path=$1
  local output_directory=$2
  local force=${3:-false}

  echo "Extracting $zip_path to $output_directory"
  if [[ -d $output_directory ]] && [[ $force = false ]]; then
    echo "Directory '$output_directory' already exists, skipping extract"
    return 0
  fi

  if [[ -d $output_directory ]]; then
    echo "'Force flag enabled, but '$output_directory' exists. Removing directory"
    rm -rf $output_directory
    if [[ $? != 0 ]]; then
      Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Unable to remove '$output_directory'"
      return 1
    fi
  fi

  echo "Creating directory: '$output_directory'"
  mkdir -p $output_directory

  echo "Extracting archive"
  tar -xf $zip_path -C $output_directory
  if [[ $? != 0 ]]; then
    Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Unable to extract '$zip_path'"
    return 1
  fi

  return 0
}

function GetCurrentOS {
  local unameOut="$(uname -s)"
  case $unameOut in
    Linux*)     echo "Linux";;
    Darwin*)    echo "MacOS";;
  esac
  return 0
}

function GetFile {
  local uri=$1
  local path=$2
  local force=${3:-false}
  local download_retries=${4:-5}
  local retry_wait_time_seconds=${5:-30}

  if [[ -f $path ]]; then
    if [[ $force = false ]]; then
      echo "File '$path' already exists. Skipping download"
      return 0
    else
      rm -rf $path
    fi
  fi

  if [[ -f $uri ]]; then
    echo "'$uri' is a file path, copying file to '$path'"
    cp $uri $path
    return $?
  fi

  echo "Downloading $uri"
  # Use curl if available, otherwise use wget
  if command -v curl > /dev/null; then
    curl "$uri" -sSL --retry $download_retries --retry-delay $retry_wait_time_seconds --create-dirs -o "$path" --fail
  else
    wget -q -O "$path" "$uri" --tries="$download_retries"
  fi

  return $?
}

function GetTempPathFileName {
  local path=$1

  local temp_dir=$(GetTempDirectory)
  local temp_file_name=$(basename $path)
  echo $temp_dir$temp_file_name
  return 0
}

function DownloadAndExtract {
  local uri=$1
  local installDir=$2
  local force=${3:-false}
  local download_retries=${4:-5}
  local retry_wait_time_seconds=${5:-30}

  local temp_tool_path=$(GetTempPathFileName $uri)

  echo "downloading to: $temp_tool_path"

  # Download file
  GetFile "$uri" "$temp_tool_path" $force $download_retries $retry_wait_time_seconds
  if [[ $? != 0 ]]; then
    Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Failed to download '$uri' to '$temp_tool_path'."
    return 1
  fi

  # Extract File
  echo "extracting from  $temp_tool_path to $installDir"
  ExpandZip "$temp_tool_path" "$installDir" $force $download_retries $retry_wait_time_seconds
  if [[ $? != 0 ]]; then
    Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Failed to extract '$temp_tool_path' to '$installDir'."
    return 1
  fi

  return 0
}

function NewScriptShim {
  local shimpath=$1
  local tool_file_path=$2
  local force=${3:-false}

  echo "Generating '$shimpath' shim"
  if [[ -f $shimpath ]]; then
    if [[ $force = false ]]; then
      echo "File '$shimpath' already exists." >&2
      return 1
    else
      rm -rf $shimpath
    fi
  fi
  
  if [[ ! -f $tool_file_path ]]; then
    # try to see if the path is lower cased
    tool_file_path="$(echo $tool_file_path | tr "[:upper:]" "[:lower:]")" 
    if [[ ! -f $tool_file_path ]]; then
      Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Specified tool file path:'$tool_file_path' does not exist"
      return 1
    fi
  fi

  local shim_contents=$'#!/usr/bin/env bash\n'
  shim_contents+="SHIMARGS="$'$1\n'
  shim_contents+="$tool_file_path"$' $SHIMARGS\n'

  # Write shim file
  echo "$shim_contents" > $shimpath

  chmod +x $shimpath

  echo "Finished generating shim '$shimpath'"

  return $?
}

