# kvm.sh
# Source this file from your .bash-profile or script to use
# With inspriation from nvm.sh

SCRIPTPATH="$_"

#Exit script when any command returns non 0 exit code

_kvm_has() {
  type "$1" > /dev/null 2>&1
  return $?
}

# Make zsh glob matching behave same as bash
# This fixes the "zsh: no matches found" errors
if _kvm_has "unsetopt"; then
  unsetopt nomatch 2>/dev/null
  KVM_CD_FLAGS="-q"
fi

if [ -z "$KRE_USER_HOME" ]; then
  eval KRE_USER_HOME=~/.kre
fi

KRE_USER_PACKAGES="$KRE_USER_HOME/packages"
KRE_MONO45=
KRE_X86=
KRE_X64=
KRE_NUGET_API_URL="https://www.myget.org/F/aspnetvnext/api/v2"

# Traverse up in directory tree to find containing folder
_kvm_find_up() {
  local path
  path=$PWD
  while [ "$path" != "" ] && [ ! -f "$path/$1" ]; do
    path=${path%/*}
  done
  echo "$path"
}

_kvm_find_kvmrc() {
  local dir="$(_kvm_find_up '.kvmrc')"
  if [ -e "$dir/.kvmrc" ]; then
        echo "$dir/.kvmrc"
  fi
}

# Obtain kvm version from rc file
_kvm_rc_version() {
  local KVMRC_PATH="$(_kvm_find_kvmrc)"
  if [ -e "$KVMRC_PATH" ]; then
    _kvm_rc_version=`cat "$KVMRC_PATH" | head -n 1`
    echo "Found '$KVMRC_PATH' with version <$_kvm_rc_version>"
  fi
}

_kvm_find_latest() {
  local platform="mono45"
  local architecture="x86"

  if ! _kvm_has "curl"; then
    echo 'KVM Needs curl to proceed.' >&2;
    return 1
  fi

  local url="$KRE_NUGET_API_URL/GetUpdates()?packageIds=%27KRE-$platform-$architecture%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false"
  local xml=$(curl -silent -L -u aspnetreadonly:4d8a2d9c-7b80-4162-9978-47e918c9658c $url)
  version=$(echo $xml | sed "s/.*<[a-zA-Z]:Version>\([^<]*\).*/\1/")
  [[ $xml == $version ]] && return 1
  echo $version
}

_kvm_strip_path() {
  echo "$1" | sed -e "s#$KRE_USER_PACKAGES/[^/]*$2[^:]*:##g" -e "s#:$KRE_USER_PACKAGES/[^/]*$2[^:]*##g" -e "s#$KRE_USER_PACKAGES/[^/]*$2[^:]*##g"
}

_kvm_prepend_path() {
  if [ -z "$1" ]; then
    echo "$2"
  else
    echo "$2:$1"
  fi
}

_kvm_download() {
  local kreFullName="$1"
  local kreFolder="$2"

  local pkgName=$(echo "$kreFullName" | sed "s/\([^.]*\).*/\1/")
  local pkgVersion=$(echo "$kreFullName" | sed "s/[^.]*.\(.*\)/\1/")
  local url="$KRE_NUGET_API_URL/package/$pkgName/$pkgVersion"
  local kreFile="$kreFolder/$kreFullName.nupkg"

  if [ -e "$kreFolder" ]; then
    echo "$kreFullName already installed."
    return 0
  fi

  echo "Downloading $kreFullName from $KRE_NUGET_API_URL"

  if ! _kvm_has "curl"; then
    echo "KVM Needs curl to proceed." >&2;
    return 1
  fi

  mkdir -p "$kreFolder" > /dev/null 2>&1

  local httpResult=$(curl -L -D - -u aspnetreadonly:4d8a2d9c-7b80-4162-9978-47e918c9658c "$url" -o "$kreFile" 2>/dev/null | grep "^HTTP/1.1" | head -n 1 | sed "s/HTTP.1.1 \([0-9]*\).*/\1/")

  [[ $httpResult == "404" ]] && echo "$kreFullName was not found in repository $KRE_NUGET_API_URL" && return 1
  [[ $httpResult != "302" ]] && echo "Http Error $httpResult fetching $kreFullName from $KRE_NUGET_API_URL" && return 1

  _kvm_unpack $kreFile $kreFolder
}

_kvm_unpack() {
  local kreFile="$1"
  local kreFolder="$2"

  echo "Installing to $kreFolder"

  if ! _kvm_has "unzip"; then
    echo "KVM Needs unzip to proceed." >&2;
    return 1
  fi

  unzip $kreFile -d $kreFolder > /dev/null 2>&1

  [ -e "$kreFolder/[Content_Types].xml" ] && rm "$kreFolder/[Content_Types].xml"

  [ -e "$kreFolder/_rels/" ] && rm -rf "$kreFolder/_rels/"

  [ -e "$kreFolder/package/" ] && rm -rf "$kreFolder/_package/"

  #Set shell commands as executable
  find "$kreFolder/bin/" -type f \
    -exec sh -c "head -c 11 {} | grep '/bin/bash' > /dev/null"  \; -print | xargs chmod 775

}

# This is not really needed. Placeholder until I get clarification on the supported platforms for mono
_kvm_requested_platform() {
  local default=$1

  [[ $KRE_MONO45 ]] && echo "mono45" && return

  echo $default
}

# Ditto - waiting for clarification on mono-x64 packages
_kvm_requested_architecture() {
  local default=$1

  [[ $KRE_X86 && $KRE_X64 ]] && echo "This command cannot accept both -x86 and -x64" && return 1

  [[ $KRE_X86 ]] && echo "x86" && return

  [[ $KRE_X64 ]] && echo "x64" && return

  echo $default
}

_kvm_requested_version_or_alias() {
  local versionOrAlias="$1"

  if [ -e "$KRE_USER_HOME/alias/$versionOrAlias.alias" ]; then
    local kreFullName=$(cat "$KRE_USER_HOME/alias/$versionOrAlias.alias")
    local pkgName=$(echo $kreFullName | sed "s/\([^.]*\).*/\1/")
    local pkgVersion=$(echo $kreFullName | sed "s/[^.]*.\(.*\)/\1/")
    local pkgPlatform=$(_kvm_requested_platform $(echo "$pkgName" | sed "s/KRE-\([^-]*\).*/\1/"))
    local pkgArchitecture=$(_kvm_requested_architecture $(echo "$pkgName" | sed "s/.*-.*-\([^-]*\).*/\1/"))
  else
    local pkgVersion=$versionOrAlias
    local pkgPlatform=$(_kvm_requested_platform "mono45")
    local pkgArchitecture=$(_kvm_requested_architecture "x86")
  fi
  echo "KRE-$pkgPlatform-$pkgArchitecture.$pkgVersion"
}

# This will be more relevant if we support global installs
_kvm_locate_kre_bin_from_full_name() {
  local kreFullName=$1

  [ -e "$KRE_USER_PACKAGES/$kreFullName/bin" ] && echo "$KRE_USER_PACKAGES/$kreFullName/bin" && return
}

kvm()
{
  if [ $# -lt 1 ]; then
    kvm help
    return
  fi

  case $1 in
    "help" )
      echo ""
      echo "K Runtime Environment Version Manager - Build {{BUILD_NUMBER}}"
      echo ""
      echo "USAGE: kvm <command> [options]"
      echo ""
      echo "kvm upgrade"
      echo "install latest KRE from feed"
      echo "set 'default' alias to installed version"
      echo "add KRE bin to user PATH environment variable persistently"
      echo ""
      echo "kvm install <semver>|<alias>|<nupkg>"
      echo "install requested KRE from feed"
      echo "add KRE bin to path of current command line"
      echo ""
      echo "kvm use <semver>|<alias>|none [-p -persistent]"
      echo "<semver>|<alias>  add KRE bin to path of current command line   "
      echo "none              remove KRE bin from path of current command line"
      echo "-p -persistent   set selected version as default"
      echo ""
      echo "kvm list"
      echo "list KRE versions installed "
      echo ""
      echo "kvm alias"
      echo "list KRE aliases which have been defined"
      echo ""
      echo "kvm alias <alias>"
      echo "display value of named alias"
      echo ""
      echo "kvm alias <alias> <semver>"
      echo "set alias to specific version"
      echo ""
      echo ""
    ;;

    "upgrade" )
      [ $# -ne 1 ] && kvm help && return
      echo "Determining latest version"
      local version=$(_kvm_find_latest mono45 x86)
      echo $version
      kvm install $version
      kvm alias default $version
    ;;

    "install" )
      [ $# -ne 2 ] && kvm help && return

      local versionOrAlias="$2"

      if [ "$versionOrAlias" == *.nupkg ]; then
        local kreFullName=$(echo $versionOrAlias | sed "s/\(.*\)\.nupkg/\1/")
        local kreFolder="$KRE_USER_PACKAGES/$kreFullName"
        local kreFile="$kreFolder/$kreFullName.nupkg"

        if [ -e "$kreFolder" ]; then
          echo "Target folder '$kreFolder' already exists"
        else
          mkdir "$kreFolder" > /dev/null 2>&1
          cp -a "$versionOrAlias" "$kreFile"
          _kvm_unpack "$kreFile" "$kreFolder"
        fi

        echo "Adding $kreBin to current PATH"
        PATH=$(_kvm_strip_path "$PATH" "/bin")
        PATH=(_kvm_prepend_path "$PATH" "$kreBin")
      else
        local kreFullName="$(_kvm_requested_version_or_alias $versionOrAlias)"
        local kreFolder="$KRE_USER_PACKAGES/$kreFullName"
        _kvm_download "$kreFullName" "$kreFolder"
        kvm use "$versionOrAlias"
      fi
    ;;

    "use" )
      [ $# -gt 3 ] && kvm help && return
      [ $# -lt 2 ] && kvm help && return

      shift
      local persistant=

      while [ $# -ne 0 ]
      do
        if [[ $1 == "-p" || $1 == "-persistant" ]]; then
          local persistant="true"
        else
          local versionOrAlias=$1
        fi
        shift
      done

      if [[ $versionOrAlias == "none" ]]; then
        echo "Removing KRE from process PATH"
        # Strip other version from PATH
        PATH=`_kvm_strip_path "$PATH" "/bin"`

        if [[ $persistent&& -e "$KRE_USER_HOME/alias/default.alias" ]]; then
            echo "Setting default KRE to none"

            rm "$KRE_USER_HOME/alias/default.alias"
        fi
        return 0
      fi

      local kreFullName=$(_kvm_requested_version_or_alias "$versionOrAlias")
      local kreBin=$(_kvm_locate_kre_bin_from_full_name "$kreFullName")

      if [[ ! $kreBin ]]; then
        echo "Cannot find $kreFullName, do you need to run 'kvm install $versionOrAlias'?"
        return 1
      fi

      echo "Adding" $kreBin "to process PATH"

      PATH=`_kvm_strip_path "$PATH" "/bin"`
      PATH=`_kvm_prepend_path "$PATH" "$kreBin"`

      if [[ $persistent ]]; then
          echo "Setting  $kreBin as default KRE"
          kvm alias default "$versionOrAlias"
      fi
    ;;

    "alias" )
      [ $# -gt 3 ] && kvm help && return

      if [[ $# == 1 ]]; then
        for f in $(find "$KRE_USER_HOME/alias" -name *.alias); do printf "%-20s %s\n" "$(basename $f | sed 's/.alias//')" "$(cat $f)"; done
        echo ""
        return;
      fi

      local name="$2"

      if [[ $# == 2 ]]; then
        [[ ! -e "$KRE_USER_HOME/alias/$name.alias" ]] && echo "There is no alias called '$name'" && return
        cat "$KRE_USER_HOME/alias/$name.alias"
        echo ""
        return
      fi

      local semver="$3"
      local kreFullName="KRE-$(_kvm_requested_platform mono45)-$(_kvm_requested_architecture x86).$semver"

      echo "Setting alias '$name' to '$kreFullName'"
      [[ ! -e "$KRE_USER_HOME/alias/" ]] && mkdir "$KRE_USER_HOME/alias/" > /dev/null

      echo "$kreFullName" > "$KRE_USER_HOME/alias/$name.alias"
    ;;

    "list" )
      [ $# -gt 2 ] && kvm help && return

      [ ! -d $KRE_USER_PACKAGES ] && echo "KRE is not installed." && return 1

      local searchGlob="KRE-*"
      if [ $# == 2 ]; then
        local versionOrAlias=$2
        local searchGlob=$(_kvm_requested_version_or_alias "$versionOrAlias")
        echo $searchGlob
      fi


      for f in $(find $KRE_USER_PACKAGES/* -name $searchGlob -type d -prune -exec basename {} \;); do
        #TODO: Format, extract package, version arch etc
        echo -n $f
        if [[ $PATH == *"$KRE_USER_PACKAGES/$f/bin"* ]]; then
          echo " *"
        else
          echo ""
        fi
        [[ $# == 2 ]] && echo "" &&  return 0
      done

      echo ""
      [[ $# == 2 ]] && return 1 # kvm list xxx - xxx was not found
  esac
}

kvm list default >/dev/null && kvm use default >/dev/null || true
