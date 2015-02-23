# kvm.sh
# Source this file from your .bash-profile or script to use

# "Constants"
_KVM_BUILDNUMBER="10319"
_KVM_RUNTIME_PACKAGE_NAME="kre"
_KVM_RUNTIME_FRIENDLY_NAME="K Runtime"
_KVM_RUNTIME_SHORT_NAME="KRE"
_KVM_RUNTIME_FOLDER_NAME=".k"
_KVM_COMMAND_NAME="kvm"
_KVM_VERSION_MANAGER_NAME="K Version Manager"
_KVM_DEFAULT_FEED="https://www.nuget.org/api/v2"
_KVM_HOME_VAR_NAME="KRE_HOME"

__kvm_has() {
    type "$1" > /dev/null 2>&1
    return $?
}

if __kvm_has "unsetopt"; then
    unsetopt nomatch 2>/dev/null
fi

if [ -z "$KVM_USER_HOME" ]; then
    eval KVM_USER_HOME="~/$_KVM_RUNTIME_FOLDER_NAME"
fi

_KVM_USER_PACKAGES="$KVM_USER_HOME/runtimes"
_KVM_ALIAS_DIR="$KVM_USER_HOME/alias"

if [ -z "$KRE_FEED" ]; then
    KRE_FEED="$_KVM_DEFAULT_FEED"
fi

__kvm_find_latest() {
    local platform="mono"

    if ! __kvm_has "curl"; then
        echo "$_KVM_COMMAND_NAME needs curl to proceed." >&2;
        return 1
    fi

    local url="$KRE_FEED/GetUpdates()?packageIds=%27$_KVM_RUNTIME_PACKAGE_NAME-$platform%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false"
    xml="$(curl $url 2>/dev/null)"
    echo $xml | grep \<[a-zA-Z]:Version\>* >> /dev/null || return 1
    version="$(echo $xml | sed 's/.*<[a-zA-Z]:Version>\([^<]*\).*/\1/')"
    echo $version
}

__kvm_strip_path() {
    echo "$1" | sed -e "s#$_KVM_USER_PACKAGES/[^/]*$2[^:]*:##g" -e "s#:$_KVM_USER_PACKAGES/[^/]*$2[^:]*##g" -e "s#$_KVM_USER_PACKAGES/[^/]*$2[^:]*##g"
}

__kvm_prepend_path() {
    if [ -z "$1" ]; then
        echo "$2"
    else
        echo "$2:$1"
    fi
}

__kvm_package_version() {
    local runtimeFullName="$1"
    echo "$runtimeFullName" | sed "s/[^.]*.\(.*\)/\1/"
}

__kvm_package_name() {
    local runtimeFullName="$1"
    echo "$runtimeFullName" | sed "s/\([^.]*\).*/\1/"
}

__kvm_package_runtime() {
    local runtimeFullName="$1"
    echo "$runtimeFullName" | sed "s/$_KVM_RUNTIME_PACKAGE_NAME-\([^.-]*\).*/\1/"
}

__kvm_download() {
    local runtimeFullName="$1"
    local runtimeFolder="$2"

    local pkgName=$(__kvm_package_name "$runtimeFullName")
    local pkgVersion=$(__kvm_package_version "$runtimeFullName")
    local url="$KRE_FEED/package/$pkgName/$pkgVersion"
    local runtimeFile="$runtimeFolder/$runtimeFullName.nupkg"

    if [ -e "$runtimeFolder" ]; then
        echo "$runtimeFullName already installed."
        return 0
    fi

    echo "Downloading $runtimeFullName from $KRE_FEED"

    if ! __kvm_has "curl"; then
        echo "$_KVM_COMMAND_NAME needs curl to proceed." >&2;
        return 1
    fi

    mkdir -p "$runtimeFolder" > /dev/null 2>&1

    local httpResult=$(curl -L -D - "$url" -o "$runtimeFile" 2>/dev/null | grep "^HTTP/1.1" | head -n 1 | sed "s/HTTP.1.1 \([0-9]*\).*/\1/")

    [[ $httpResult == "404" ]] && echo "$runtimeFullName was not found in repository $KRE_FEED" && return 1
    [[ $httpResult != "302" && $httpResult != "200" ]] && echo "HTTP Error $httpResult fetching $runtimeFullName from $KRE_FEED" && return 1

    __kvm_unpack $runtimeFile $runtimeFolder
    return $?
}

__kvm_unpack() {
    local runtimeFile="$1"
    local runtimeFolder="$2"

    echo "Installing to $runtimeFolder"

    if ! __kvm_has "unzip"; then
        echo "$_KVM_COMMAND_NAME needs unzip to proceed." >&2;
        return 1
    fi

    unzip $runtimeFile -d $runtimeFolder > /dev/null 2>&1

    [ -e "$runtimeFolder/[Content_Types].xml" ] && rm "$runtimeFolder/[Content_Types].xml"

    [ -e "$runtimeFolder/_rels/" ] && rm -rf "$runtimeFolder/_rels/"

    [ -e "$runtimeFolder/package/" ] && rm -rf "$runtimeFolder/_package/"

    [ -e "$runtimeFile" ] && rm -f "$runtimeFile"

    #Set shell commands as executable
    find "$runtimeFolder/bin/" -type f \
        -exec sh -c "head -c 11 {} | grep '/bin/bash' > /dev/null"  \; -print | xargs chmod 775
}

__kvm_requested_version_or_alias() {
    local versionOrAlias="$1"
    local runtimeBin=$(__kvm_locate_runtime_bin_from_full_name "$versionOrAlias")

    # If the name specified is an existing package, just use it as is
    if [ -n "$runtimeBin" ]; then
        echo "$versionOrAlias"
    else
       if [ -e "$_KVM_ALIAS_DIR/$versionOrAlias.alias" ]; then
           local runtimeFullName=$(cat "$_KVM_ALIAS_DIR/$versionOrAlias.alias")
           local pkgName=$(echo $runtimeFullName | sed "s/\([^.]*\).*/\1/")
           local pkgVersion=$(echo $runtimeFullName | sed "s/[^.]*.\(.*\)/\1/")
           local pkgPlatform=$(echo "$pkgName" | sed "s/kre-\([^.-]*\).*/\1/")
        else
            local pkgVersion=$versionOrAlias
            local pkgPlatform="mono"
        fi

        echo "$_KVM_RUNTIME_PACKAGE_NAME-$pkgPlatform.$pkgVersion"
    fi
}

# This will be more relevant if we support global installs
__kvm_locate_runtime_bin_from_full_name() {
    local runtimeFullName=$1
    [ -e "$_KVM_USER_PACKAGES/$runtimeFullName/bin" ] && echo "$_KVM_USER_PACKAGES/$runtimeFullName/bin" && return
}

kvm()
{
    if [ $# -lt 1 ]; then
        $_KVM_COMMAND_NAME help
        return
    fi

    case $1 in
        "help" )
            echo ""
            echo "$_KVM_VERSION_MANAGER_NAME - Build $_KVM_BUILDNUMBER"
            echo ""
            echo "USAGE: $_KVM_COMMAND_NAME <command> [options]"
            echo ""
            echo "$_KVM_COMMAND_NAME upgrade"
            echo "install latest $_KVM_RUNTIME_SHORT_NAME from feed"
            echo "add $_KVM_RUNTIME_SHORT_NAME bin to path of current command line"
            echo "set installed version as default"
            echo ""
            echo "$_KVM_COMMAND_NAME install <semver>|<alias>|<nupkg>|latest [-a|-alias <alias>] [-p -persistent]"
            echo "<semver>|<alias>  install requested $_KVM_RUNTIME_SHORT_NAME from feed"
            echo "<nupkg>           install requested $_KVM_RUNTIME_SHORT_NAME from local package on filesystem"
            echo "latest            install latest version of $_KVM_RUNTIME_SHORT_NAME from feed"
            echo "-a|-alias <alias> set alias <alias> for requested $_KVM_RUNTIME_SHORT_NAME on install"
            echo "-p -persistent    set installed version as default"
            echo "add $_KVM_RUNTIME_SHORT_NAME bin to path of current command line"
            echo ""
            echo "$_KVM_COMMAND_NAME use <semver>|<alias>|<package>|none [-p -persistent]"
            echo "<semver>|<alias>|<package>  add $_KVM_RUNTIME_SHORT_NAME bin to path of current command line   "
            echo "none                        remove $_KVM_RUNTIME_SHORT_NAME bin from path of current command line"
            echo "-p -persistent              set selected version as default"
            echo ""
            echo "$_KVM_COMMAND_NAME list"
            echo "list $_KVM_RUNTIME_SHORT_NAME versions installed "
            echo ""
            echo "$_KVM_COMMAND_NAME alias"
            echo "list $_KVM_RUNTIME_SHORT_NAME aliases which have been defined"
            echo ""
            echo "$_KVM_COMMAND_NAME alias <alias>"
            echo "display value of the specified alias"
            echo ""
            echo "$_KVM_COMMAND_NAME alias <alias> <semver>|<alias>|<package>"
            echo "<alias>                      the name of the alias to set"
            echo "<semver>|<alias>|<package>   the $_KVM_RUNTIME_SHORT_NAME version to set the alias to. Alternatively use the version of the specified alias"
            echo ""
            echo "$_KVM_COMMAND_NAME unalias <alias>"
            echo "remove the specified alias"
            echo ""
        ;;

        "upgrade" )
            [ $# -ne 1 ] && kvm help && return
            $_KVM_COMMAND_NAME install latest -p
        ;;

        "install" )
            [ $# -lt 2 ] && kvm help && return
            shift
            local persistent=
            local versionOrAlias=
            local alias=
            while [ $# -ne 0 ]
            do
                if [[ $1 == "-p" || $1 == "-persistent" ]]; then
                    local persistent="-p"
                elif [[ $1 == "-a" || $1 == "-alias" ]]; then
                    local alias=$2
                    shift
                elif [[ -n $1 ]]; then
                    [[ -n $versionOrAlias ]] && echo "Invalid option $1" && kvm help && return 1
                    local versionOrAlias=$1
                fi
                shift
            done
            if [[ "$versionOrAlias" == "latest" ]]; then
                echo "Determining latest version"
                versionOrAlias=$(__kvm_find_latest)
                [[ $? == 1 ]] && echo "Error: Could not find latest version from feed $KRE_FEED" && return 1
                echo "Latest version is $versionOrAlias"
            fi
            if [[ "$versionOrAlias" == *.nupkg ]]; then
                local runtimeFullName=$(basename $versionOrAlias | sed "s/\(.*\)\.nupkg/\1/")
                local runtimeVersion=$(__kvm_package_version "$runtimeFullName")
                local runtimeFolder="$_KVM_USER_PACKAGES/$runtimeFullName"
                local runtimeFile="$runtimeFolder/$runtimeFullName.nupkg"

                if [ -e "$runtimeFolder" ]; then
                  echo "$runtimeFullName already installed"
                else
                  mkdir "$runtimeFolder" > /dev/null 2>&1
                  cp -a "$versionOrAlias" "$runtimeFile"
                  __kvm_unpack "$runtimeFile" "$runtimeFolder"
                  [[ $? == 1 ]] && return 1
                fi
                $_KVM_COMMAND_NAME use "$runtimeVersion" "$persistent"
                [[ -n $alias ]] && kvm alias "$alias" "$runtimeVersion"
            else
                local runtimeFullName="$(__kvm_requested_version_or_alias $versionOrAlias)"
                local runtimeFolder="$_KVM_USER_PACKAGES/$runtimeFullName"
                __kvm_download "$runtimeFullName" "$runtimeFolder"
                [[ $? == 1 ]] && return 1
                $_KVM_COMMAND_NAME use "$versionOrAlias" "$persistent"
                [[ -n $alias ]] && kvm alias "$alias" "$versionOrAlias"
            fi
        ;;

        "use" )
            [ $# -gt 3 ] && $_KVM_COMMAND_NAME help && return
            [ $# -lt 2 ] && $_KVM_COMMAND_NAME help && return

            shift
            local persistent=
            while [ $# -ne 0 ]
            do
                if [[ $1 == "-p" || $1 == "-persistent" ]]; then
                    local persistent="true"
                elif [[ -n $1 ]]; then
                    local versionOrAlias=$1
                fi
                shift
            done

            if [[ $versionOrAlias == "none" ]]; then
                echo "Removing $_KVM_RUNTIME_SHORT_NAME from process PATH"
                # Strip other version from PATH
                PATH=$(__kvm_strip_path "$PATH" "/bin")

                if [[ -n $persistent && -e "$_KVM_ALIAS_DIR/default.alias" ]]; then
                    echo "Setting default $_KVM_RUNTIME_SHORT_NAME to none"
                    rm "$_KVM_ALIAS_DIR/default.alias"
                fi
                return 0
            fi

            local runtimeFullName=$(__kvm_requested_version_or_alias "$versionOrAlias")
            local runtimeBin=$(__kvm_locate_runtime_bin_from_full_name "$runtimeFullName")

            if [[ -z $runtimeBin ]]; then
                echo "Cannot find $runtimeFullName, do you need to run '$_KVM_COMMAND_NAME install $versionOrAlias'?"
                return 1
            fi

            echo "Adding" $runtimeBin "to process PATH"

            PATH=$(__kvm_strip_path "$PATH" "/bin")
            PATH=$(__kvm_prepend_path "$PATH" "$runtimeBin")

            if [[ -n $persistent ]]; then
                local runtimeVersion=$(__kvm_package_version "$runtimeFullName")
                $_KVM_COMMAND_NAME alias default "$runtimeVersion"
            fi
        ;;

        "alias" )
            [[ $# -gt 3 ]] && kvm help && return

            [[ ! -e "$_KVM_ALIAS_DIR/" ]] && mkdir "$_KVM_ALIAS_DIR/" > /dev/null

            if [[ $# == 1 ]]; then
                echo ""
                local format="%-20s %s\n"
                printf "$format" "Alias" "Name"
                printf "$format" "-----" "----"
                if [ -d "$_KVM_ALIAS_DIR" ]; then
                    for __kvm_file in $(find "$_KVM_ALIAS_DIR" -name *.alias); do
                        local alias="$(basename $__kvm_file | sed 's/\.alias//')"
                        local name="$(cat $__kvm_file)"
                        printf "$format" "$alias" "$name"
                    done
                fi
                echo ""
                return
            fi

            local name="$2"

            if [[ $# == 2 ]]; then
                [[ ! -e "$_KVM_ALIAS_DIR/$name.alias" ]] && echo "There is no alias called '$name'" && return
                cat "$_KVM_ALIAS_DIR/$name.alias"
                echo ""
                return
            fi

            local runtimeFullName=$(__kvm_requested_version_or_alias "$3")

            [[ ! -d "$_KVM_USER_PACKAGES/$runtimeFullName" ]] && echo "$runtimeFullName is not an installed $_KVM_RUNTIME_SHORT_NAME version" && return 1

            local action="Setting"
            [[ -e "$_KVM_ALIAS_DIR/$name.alias" ]] && action="Updating"
            echo "$action alias '$name' to '$runtimeFullName'"
            echo "$runtimeFullName" > "$_KVM_ALIAS_DIR/$name.alias"
        ;;

        "unalias" )
            [[ $# -ne 2 ]] && kvm help && return

            local name=$2
            local aliasPath="$_KVM_ALIAS_DIR/$name.alias"
            [[ ! -e  "$aliasPath" ]] && echo "Cannot remove alias, '$name' is not a valid alias name" && return 1
            echo "Removing alias $name"
            rm "$aliasPath" >> /dev/null 2>&1
        ;;

        "list" )
            [[ $# -gt 2 ]] && kvm help && return

            [[ ! -d $_KVM_USER_PACKAGES ]] && echo "$_KVM_RUNTIME_FRIENDLY_NAME is not installed." && return 1

            local searchGlob="$_KVM_RUNTIME_PACKAGE_NAME-*"
            if [ $# == 2 ]; then
                local versionOrAlias=$2
                local searchGlob=$(__kvm_requested_version_or_alias "$versionOrAlias")
            fi
            echo ""

            # Separate empty array declaration from initialization
            # to avoid potential ZSH error: local:217: maximum nested function level reached
            local arr
            arr=()

            # Z shell array-index starts at one.
            local i=1
            local format="%-20s %s\n"
            if [ -d "$_KVM_ALIAS_DIR" ]; then
                for __kvm_file in $(find "$_KVM_ALIAS_DIR" -name *.alias); do
                    arr[$i]="$(basename $__kvm_file | sed 's/\.alias//')/$(cat $__kvm_file)"
                    let i+=1
                done
            fi

            local formatString="%-6s %-20s %-7s %-20s %s\n"
            printf "$formatString" "Active" "Version" "Runtime" "Location" "Alias"
            printf "$formatString" "------" "-------" "-------" "--------" "-----"

            local formattedHome=`(echo $_KVM_USER_PACKAGES | sed s=$HOME=~=g)`
            for f in $(find $_KVM_USER_PACKAGES -name "$searchGlob" \( -type d -or -type l \) -prune -exec basename {} \;); do
                local active=""
                [[ $PATH == *"$_KVM_USER_PACKAGES/$f/bin"* ]] && local active="  *"
                local pkgName=$(__kvm_package_runtime "$f")
                local pkgVersion=$(__kvm_package_version "$f")

                local alias=""
                local delim=""
                for i in "${arr[@]}"; do
                    temp="$_KVM_RUNTIME_PACKAGE_NAME-$pkgName.$pkgVersion"
                    temp2="$_KVM_RUNTIME_PACKAGE_NAME-$pkgName-x86.$pkgVersion"
                    if [[ ${i#*/} == $temp || ${i#*/} == $temp2 ]]; then
                        alias+="$delim${i%/*}"
                        delim=", "
                    fi
                done

                printf "$formatString" "$active" "$pkgVersion" "$pkgName" "$formattedHome" "$alias"
                [[ $# == 2 ]] && echo "" &&  return 0
            done

            echo ""
            [[ $# == 2 ]] && echo "$versionOrAlias not found" && return 1
        ;;

        *)
            echo "Unknown command $1"
            return 1
    esac

    return 0
}

# Generate the command function using the constant defined above.
$_KVM_COMMAND_NAME list default >/dev/null && $_KVM_COMMAND_NAME use default >/dev/null || true
