#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

base_uri='https://netcorenativeassets.blob.core.windows.net/resource-packages/external'
install_directory=''
clean=false
force=false
download_retries=5
retry_wait_time_seconds=30
global_json_file="$(dirname "$(dirname "${scriptroot}")")/global.json"
declare -A native_assets

. $scriptroot/pipeline-logging-functions.sh
. $scriptroot/native/common-library.sh

while (($# > 0)); do
  lowerI="$(echo $1 | tr "[:upper:]" "[:lower:]")"
  case $lowerI in
    --baseuri)
      base_uri=$2
      shift 2
      ;;
    --installdirectory)
      install_directory=$2
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
    --donotabortonfailure)
      donotabortonfailure=true
      shift 1
      ;;
    --donotdisplaywarnings)
      donotdisplaywarnings=true
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
      echo "  --installdirectory                  Directory to install native toolset."
      echo "                                      This is a command-line override for the default"
      echo "                                      Install directory precedence order:"
      echo "                                          - InstallDirectory command-line override"
      echo "                                          - NETCOREENG_INSTALL_DIRECTORY environment variable"
      echo "                                          - (default) %USERPROFILE%/.netcoreeng/native"
      echo ""
      echo "  --clean                             Switch specifying not to install anything, but cleanup native asset folders"
      echo "  --donotabortonfailure               Switch specifiying whether to abort native tools installation on failure"
      echo "  --donotdisplaywarnings              Switch specifiying whether to display warnings during native tools installation on failure"
      echo "  --force                             Clean and then install tools"
      echo "  --help                              Print help and exit"
      echo ""
      echo "Advanced settings:"
      echo "  --baseuri <value>                   Base URI for where to download native tools from"
      echo "  --downloadretries <value>           Number of times a download should be attempted"
      echo "  --retrywaittimeseconds <value>      Wait time between download attempts"
      echo ""
      exit 0
      ;;
  esac
done

function ReadGlobalJsonNativeTools {
  # happy path: we have a proper JSON parsing tool `jq(1)` in PATH!
  if command -v jq &> /dev/null; then

    # jq: read each key/value pair under "native-tools" entry and emit:
    #   KEY="<entry-key>" VALUE="<entry-value>"
    # followed by a null byte.
    #
    # bash: read line with null byte delimeter and push to array (for later `eval`uation).

    while IFS= read -rd '' line; do
      native_assets+=("$line")
    done < <(jq -r '. |
        select(has("native-tools")) |
        ."native-tools" |
        keys[] as $k |
        @sh "KEY=\($k) VALUE=\(.[$k])\u0000"' "$global_json_file")

    return
  fi

  # Warning: falling back to manually parsing JSON, which is not recommended.

  # Following routine matches the output and escaping logic of jq(1)'s @sh formatter used above.
  # It has been tested with several weird strings with escaped characters in entries (key and value)
  # and results were compared with the output of jq(1) in binary representation using xxd(1);
  # just before the assignment to 'native_assets' array (above and below).

  # try to capture the section under "native-tools".
  if [[ ! "$(cat "$global_json_file")" =~ \"native-tools\"[[:space:]\:\{]*([^\}]+) ]]; then
    return
  fi

  section="${BASH_REMATCH[1]}"

  parseStarted=0
  possibleEnd=0
  escaping=0
  escaped=0
  isKey=1

  for (( i=0; i<${#section}; i++ )); do
    char="${section:$i:1}"
    if ! ((parseStarted)) && [[ "$char" =~ [[:space:],:] ]]; then continue; fi

    if ! ((escaping)) && [[ "$char" == "\\" ]]; then
      escaping=1
    elif ((escaping)) && ! ((escaped)); then
      escaped=1
    fi

    if ! ((parseStarted)) && [[ "$char" == "\"" ]]; then
      parseStarted=1
      possibleEnd=0
    elif [[ "$char" == "'" ]]; then
      token="$token'\\\''"
      possibleEnd=0
    elif ((escaping)) || [[ "$char" != "\"" ]]; then
      token="$token$char"
      possibleEnd=1
    fi

    if ((possibleEnd)) && ! ((escaping)) && [[ "$char" == "\"" ]]; then
      # Use printf to unescape token to match jq(1)'s @sh formatting rules.
      # do not use 'token="$(printf "$token")"' syntax, as $() eats the trailing linefeed.
      printf -v token "'$token'"

      if ((isKey)); then
        KEY="$token"
        isKey=0
      else
        line="KEY=$KEY VALUE=$token"
        native_assets+=("$line")
        isKey=1
      fi

      # reset for next token
      parseStarted=0
      token=
    elif ((escaping)) && ((escaped)); then
      escaping=0
      escaped=0
    fi
  done
}

native_base_dir=$install_directory
if [[ -z $install_directory ]]; then
  native_base_dir=$(GetNativeInstallDirectory)
fi

install_bin="${native_base_dir}/bin"
installed_any=false

ReadGlobalJsonNativeTools

if [[ ${#native_assets[@]} -eq 0 ]]; then
  echo "No native tools defined in global.json"
  exit 0;
else
  native_installer_dir="$scriptroot/native"
  for index in "${!native_assets[@]}"; do
    eval "${native_assets["$index"]}"

    installer_path="$native_installer_dir/install-$KEY.sh"
    installer_command="$installer_path"
    installer_command+=" --baseuri $base_uri"
    installer_command+=" --installpath $install_bin"
    installer_command+=" --version $VALUE"
    echo $installer_command

    if [[ $force = true ]]; then
      installer_command+=" --force"
    fi

    if [[ $clean = true ]]; then
      installer_command+=" --clean"
    fi

    if [[ -a $installer_path ]]; then
      $installer_command
      if [[ $? != 0 ]]; then
        if [[ $donotabortonfailure = true ]]; then
          if [[ $donotdisplaywarnings != true ]]; then
            Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Execution Failed"
          fi
        else
          Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Execution Failed"
          exit 1
        fi
      else
        $installed_any = true
      fi
    else
      if [[ $donotabortonfailure == true ]]; then
        if [[ $donotdisplaywarnings != true ]]; then
          Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Execution Failed: no install script"
        fi
      else
        Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Execution Failed: no install script"
        exit 1
      fi
    fi
  done
fi

if [[ $clean = true ]]; then
  exit 0
fi

if [[ -d $install_bin ]]; then
  echo "Native tools are available from $install_bin"
  echo "##vso[task.prependpath]$install_bin"
else
  if [[ $installed_any = true ]]; then
    Write-PipelineTelemetryError -category 'NativeToolsBootstrap' "Native tools install directory does not exist, installation failed"
    exit 1
  fi
fi

exit 0
