# dnvm.sh
# Source this file from your .bash-profile or script to use

# "Constants"
_DNVM_BUILDNUMBER="beta4-10345"
_DNVM_AUTHORS="Microsoft Open Technologies, Inc."
_DNVM_RUNTIME_PACKAGE_NAME="dnx"
_DNVM_RUNTIME_FRIENDLY_NAME=".NET Execution Environment"
_DNVM_RUNTIME_SHORT_NAME="DNX"
_DNVM_RUNTIME_FOLDER_NAME=".dnx"
_DNVM_COMMAND_NAME="dnvm"
_DNVM_VERSION_MANAGER_NAME=".NET Version Manager"
_DNVM_DEFAULT_FEED="https://www.myget.org/F/aspnetvnext/api/v2"
_DNVM_HOME_VAR_NAME="DNX_HOME"

[ "$_DNVM_BUILDNUMBER" = "{{*" ] && _DNVM_BUILDNUMBER="HEAD"

__dnvm_has() {
    type "$1" > /dev/null 2>&1
    return $?
}

if __dnvm_has "unsetopt"; then
    unsetopt nomatch 2>/dev/null
fi

if [ -z "$DNX_USER_HOME" ]; then
    eval DNX_USER_HOME="~/$_DNVM_RUNTIME_FOLDER_NAME"
fi

_DNVM_USER_PACKAGES="$DNX_USER_HOME/runtimes"
_DNVM_ALIAS_DIR="$DNX_USER_HOME/alias"

if [ -z "$DNX_FEED" ]; then
    DNX_FEED="$_DNVM_DEFAULT_FEED"
fi

__dnvm_find_latest() {
    local platform="mono"

    if ! __dnvm_has "curl"; then
        echo "$_DNVM_COMMAND_NAME needs curl to proceed." >&2;
        return 1
    fi

    local url="$DNX_FEED/GetUpdates()?packageIds=%27$_DNVM_RUNTIME_PACKAGE_NAME-$platform%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false"
    xml="$(curl $url 2>/dev/null)"
    echo $xml | grep \<[a-zA-Z]:Version\>* >> /dev/null || return 1
    version="$(echo $xml | sed 's/.*<[a-zA-Z]:Version>\([^<]*\).*/\1/')"
    echo $version
}

__dnvm_strip_path() {
    echo "$1" | sed -e "s#$_DNVM_USER_PACKAGES/[^/]*$2[^:]*:##g" -e "s#:$_DNVM_USER_PACKAGES/[^/]*$2[^:]*##g" -e "s#$_DNVM_USER_PACKAGES/[^/]*$2[^:]*##g"
}

__dnvm_prepend_path() {
    if [ -z "$1" ]; then
        echo "$2"
    else
        echo "$2:$1"
    fi
}

__dnvm_package_version() {
    local runtimeFullName="$1"
    echo "$runtimeFullName" | sed "s/[^.]*.\(.*\)/\1/"
}

__dnvm_package_name() {
    local runtimeFullName="$1"
    echo "$runtimeFullName" | sed "s/\([^.]*\).*/\1/"
}

__dnvm_package_runtime() {
    local runtimeFullName="$1"
    echo "$runtimeFullName" | sed "s/$_DNVM_RUNTIME_PACKAGE_NAME-\([^.-]*\).*/\1/"
}

__dnvm_download() {
    local runtimeFullName="$1"
    local runtimeFolder="$2"

    local pkgName=$(__dnvm_package_name "$runtimeFullName")
    local pkgVersion=$(__dnvm_package_version "$runtimeFullName")
    local url="$DNX_FEED/package/$pkgName/$pkgVersion"
    local runtimeFile="$runtimeFolder/$runtimeFullName.nupkg"

    if [ -e "$runtimeFolder" ]; then
        echo "$runtimeFullName already installed."
        return 0
    fi

    echo "Downloading $runtimeFullName from $DNX_FEED"

    if ! __dnvm_has "curl"; then
        echo "$_DNVM_COMMAND_NAME needs curl to proceed." >&2;
        return 1
    fi

    mkdir -p "$runtimeFolder" > /dev/null 2>&1

    local httpResult=$(curl -L -D - "$url" -o "$runtimeFile" 2>/dev/null | grep "^HTTP/1.1" | head -n 1 | sed "s/HTTP.1.1 \([0-9]*\).*/\1/")

    [[ $httpResult == "404" ]] && echo "$runtimeFullName was not found in repository $DNX_FEED" && return 1
    [[ $httpResult != "302" && $httpResult != "200" ]] && echo "HTTP Error $httpResult fetching $runtimeFullName from $DNX_FEED" && return 1

    __dnvm_unpack $runtimeFile $runtimeFolder
    return $?
}

__dnvm_unpack() {
    local runtimeFile="$1"
    local runtimeFolder="$2"

    echo "Installing to $runtimeFolder"

    if ! __dnvm_has "unzip"; then
        echo "$_DNVM_COMMAND_NAME needs unzip to proceed." >&2;
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

__dnvm_requested_version_or_alias() {
    local versionOrAlias="$1"
    local runtimeBin=$(__dnvm_locate_runtime_bin_from_full_name "$versionOrAlias")

    # If the name specified is an existing package, just use it as is
    if [ -n "$runtimeBin" ]; then
        echo "$versionOrAlias"
    else
       if [ -e "$_DNVM_ALIAS_DIR/$versionOrAlias.alias" ]; then
           local runtimeFullName=$(cat "$_DNVM_ALIAS_DIR/$versionOrAlias.alias")
           local pkgName=$(echo $runtimeFullName | sed "s/\([^.]*\).*/\1/")
           local pkgVersion=$(echo $runtimeFullName | sed "s/[^.]*.\(.*\)/\1/")
           local pkgPlatform=$(echo "$pkgName" | sed "s/$_DNVM_RUNTIME_PACKAGE_NAME-\([^.-]*\).*/\1/")
        else
            local pkgVersion=$versionOrAlias
            local pkgPlatform="mono"
        fi

        echo "$_DNVM_RUNTIME_PACKAGE_NAME-$pkgPlatform.$pkgVersion"
    fi
}

# This will be more relevant if we support global installs
__dnvm_locate_runtime_bin_from_full_name() {
    local runtimeFullName=$1
    [ -e "$_DNVM_USER_PACKAGES/$runtimeFullName/bin" ] && echo "$_DNVM_USER_PACKAGES/$runtimeFullName/bin" && return
}

__dnvm_help() {
    echo ""
    echo "$_DNVM_VERSION_MANAGER_NAME - Version 1.0.0-$_DNVM_BUILDNUMBER"
    [ "$_DNVM_AUTHORS" != "{{*" ] && echo "By $_DNVM_AUTHORS"
    echo ""
    echo "USAGE: $_DNVM_COMMAND_NAME <command> [options]"
    echo ""
    echo "$_DNVM_COMMAND_NAME upgrade"
    echo "install latest $_DNVM_RUNTIME_SHORT_NAME from feed"
    echo "add $_DNVM_RUNTIME_SHORT_NAME bin to path of current command line"
    echo "set installed version as default"
    echo ""
    echo "$_DNVM_COMMAND_NAME install <semver>|<alias>|<nupkg>|latest [-a|-alias <alias>] [-p -persistent]"
    echo "<semver>|<alias>  install requested $_DNVM_RUNTIME_SHORT_NAME from feed"
    echo "<nupkg>           install requested $_DNVM_RUNTIME_SHORT_NAME from local package on filesystem"
    echo "latest            install latest version of $_DNVM_RUNTIME_SHORT_NAME from feed"
    echo "-a|-alias <alias> set alias <alias> for requested $_DNVM_RUNTIME_SHORT_NAME on install"
    echo "-p -persistent    set installed version as default"
    echo "add $_DNVM_RUNTIME_SHORT_NAME bin to path of current command line"
    echo ""
    echo "$_DNVM_COMMAND_NAME use <semver>|<alias>|<package>|none [-p -persistent]"
    echo "<semver>|<alias>|<package>  add $_DNVM_RUNTIME_SHORT_NAME bin to path of current command line   "
    echo "none                        remove $_DNVM_RUNTIME_SHORT_NAME bin from path of current command line"
    echo "-p -persistent              set selected version as default"
    echo ""
    echo "$_DNVM_COMMAND_NAME list"
    echo "list $_DNVM_RUNTIME_SHORT_NAME versions installed "
    echo ""
    echo "$_DNVM_COMMAND_NAME alias"
    echo "list $_DNVM_RUNTIME_SHORT_NAME aliases which have been defined"
    echo ""
    echo "$_DNVM_COMMAND_NAME alias <alias>"
    echo "display value of the specified alias"
    echo ""
    echo "$_DNVM_COMMAND_NAME alias <alias> <semver>|<alias>|<package>"
    echo "<alias>                      the name of the alias to set"
    echo "<semver>|<alias>|<package>   the $_DNVM_RUNTIME_SHORT_NAME version to set the alias to. Alternatively use the version of the specified alias"
    echo ""
    echo "$_DNVM_COMMAND_NAME unalias <alias>"
    echo "remove the specified alias"
    echo ""
}

dnvm()
{
    if [ $# -lt 1 ]; then
        __dnvm_help
        return
    fi

    case $1 in
        "help" )
            __dnvm_help
        ;;

        "upgrade" )
            [ $# -ne 1 ] && __dnvm_help && return
            $_DNVM_COMMAND_NAME install latest -p
        ;;

        "install" )
            [ $# -lt 2 ] && __dnvm_help && return
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
                    [[ -n $versionOrAlias ]] && echo "Invalid option $1" && __dnvm_help && return 1
                    local versionOrAlias=$1
                fi
                shift
            done
            if [[ "$versionOrAlias" == "latest" ]]; then
                echo "Determining latest version"
                versionOrAlias=$(__dnvm_find_latest)
                [[ $? == 1 ]] && echo "Error: Could not find latest version from feed $DNX_FEED" && return 1
                echo "Latest version is $versionOrAlias"
            fi
            if [[ "$versionOrAlias" == *.nupkg ]]; then
                local runtimeFullName=$(basename $versionOrAlias | sed "s/\(.*\)\.nupkg/\1/")
                local runtimeVersion=$(__dnvm_package_version "$runtimeFullName")
                local runtimeFolder="$_DNVM_USER_PACKAGES/$runtimeFullName"
                local runtimeFile="$runtimeFolder/$runtimeFullName.nupkg"

                if [ -e "$runtimeFolder" ]; then
                  echo "$runtimeFullName already installed"
                else
                  mkdir "$runtimeFolder" > /dev/null 2>&1
                  cp -a "$versionOrAlias" "$runtimeFile"
                  __dnvm_unpack "$runtimeFile" "$runtimeFolder"
                  [[ $? == 1 ]] && return 1
                fi
                $_DNVM_COMMAND_NAME use "$runtimeVersion" "$persistent"
                [[ -n $alias ]] && $_DNVM_COMMAND_NAME alias "$alias" "$runtimeVersion"
            else
                local runtimeFullName="$(__dnvm_requested_version_or_alias $versionOrAlias)"
                local runtimeFolder="$_DNVM_USER_PACKAGES/$runtimeFullName"
                __dnvm_download "$runtimeFullName" "$runtimeFolder"
                [[ $? == 1 ]] && return 1
                $_DNVM_COMMAND_NAME use "$versionOrAlias" "$persistent"
                [[ -n $alias ]] && $_DNVM_COMMAND_NAME alias "$alias" "$versionOrAlias"
            fi
        ;;

        "use" )
            [ $# -gt 3 ] && __dnvm_help && return
            [ $# -lt 2 ] && __dnvm_help && return

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
                echo "Removing $_DNVM_RUNTIME_SHORT_NAME from process PATH"
                # Strip other version from PATH
                PATH=$(__dnvm_strip_path "$PATH" "/bin")

                if [[ -n $persistent && -e "$_DNVM_ALIAS_DIR/default.alias" ]]; then
                    echo "Setting default $_DNVM_RUNTIME_SHORT_NAME to none"
                    rm "$_DNVM_ALIAS_DIR/default.alias"
                fi
                return 0
            fi

            local runtimeFullName=$(__dnvm_requested_version_or_alias "$versionOrAlias")
            local runtimeBin=$(__dnvm_locate_runtime_bin_from_full_name "$runtimeFullName")

            if [[ -z $runtimeBin ]]; then
                echo "Cannot find $runtimeFullName, do you need to run '$_DNVM_COMMAND_NAME install $versionOrAlias'?"
                return 1
            fi

            echo "Adding" $runtimeBin "to process PATH"

            PATH=$(__dnvm_strip_path "$PATH" "/bin")
            PATH=$(__dnvm_prepend_path "$PATH" "$runtimeBin")

            if [[ -n $persistent ]]; then
                local runtimeVersion=$(__dnvm_package_version "$runtimeFullName")
                $_DNVM_COMMAND_NAME alias default "$runtimeVersion"
            fi
        ;;

        "alias" )
            [[ $# -gt 3 ]] && __dnvm_help && return

            [[ ! -e "$_DNVM_ALIAS_DIR/" ]] && mkdir "$_DNVM_ALIAS_DIR/" > /dev/null

            if [[ $# == 1 ]]; then
                echo ""
                local format="%-20s %s\n"
                printf "$format" "Alias" "Name"
                printf "$format" "-----" "----"
                if [ -d "$_DNVM_ALIAS_DIR" ]; then
                    for __dnvm_file in $(find "$_DNVM_ALIAS_DIR" -name *.alias); do
                        local alias="$(basename $__dnvm_file | sed 's/\.alias//')"
                        local name="$(cat $__dnvm_file)"
                        printf "$format" "$alias" "$name"
                    done
                fi
                echo ""
                return
            fi

            local name="$2"

            if [[ $# == 2 ]]; then
                [[ ! -e "$_DNVM_ALIAS_DIR/$name.alias" ]] && echo "There is no alias called '$name'" && return
                cat "$_DNVM_ALIAS_DIR/$name.alias"
                echo ""
                return
            fi

            local runtimeFullName=$(__dnvm_requested_version_or_alias "$3")

            [[ ! -d "$_DNVM_USER_PACKAGES/$runtimeFullName" ]] && echo "$runtimeFullName is not an installed $_DNVM_RUNTIME_SHORT_NAME version" && return 1

            local action="Setting"
            [[ -e "$_DNVM_ALIAS_DIR/$name.alias" ]] && action="Updating"
            echo "$action alias '$name' to '$runtimeFullName'"
            echo "$runtimeFullName" > "$_DNVM_ALIAS_DIR/$name.alias"
        ;;

        "unalias" )
            [[ $# -ne 2 ]] && __dnvm_help && return

            local name=$2
            local aliasPath="$_DNVM_ALIAS_DIR/$name.alias"
            [[ ! -e  "$aliasPath" ]] && echo "Cannot remove alias, '$name' is not a valid alias name" && return 1
            echo "Removing alias $name"
            rm "$aliasPath" >> /dev/null 2>&1
        ;;

        "list" )
            [[ $# -gt 2 ]] && __dnvm_help && return

            [[ ! -d $_DNVM_USER_PACKAGES ]] && echo "$_DNVM_RUNTIME_FRIENDLY_NAME is not installed." && return 1

            local searchGlob="$_DNVM_RUNTIME_PACKAGE_NAME-*"
            if [ $# == 2 ]; then
                local versionOrAlias=$2
                local searchGlob=$(__dnvm_requested_version_or_alias "$versionOrAlias")
            fi
            echo ""

            # Separate empty array declaration from initialization
            # to avoid potential ZSH error: local:217: maximum nested function level reached
            local arr
            arr=()

            # Z shell array-index starts at one.
            local i=1
            local format="%-20s %s\n"
            if [ -d "$_DNVM_ALIAS_DIR" ]; then
                for __dnvm_file in $(find "$_DNVM_ALIAS_DIR" -name *.alias); do
                    arr[$i]="$(basename $__dnvm_file | sed 's/\.alias//')/$(cat $__dnvm_file)"
                    let i+=1
                done
            fi

            local formatString="%-6s %-20s %-7s %-20s %s\n"
            printf "$formatString" "Active" "Version" "Runtime" "Location" "Alias"
            printf "$formatString" "------" "-------" "-------" "--------" "-----"

            local formattedHome=`(echo $_DNVM_USER_PACKAGES | sed s=$HOME=~=g)`
            for f in $(find $_DNVM_USER_PACKAGES -name "$searchGlob" \( -type d -or -type l \) -prune -exec basename {} \;); do
                local active=""
                [[ $PATH == *"$_DNVM_USER_PACKAGES/$f/bin"* ]] && local active="  *"
                local pkgName=$(__dnvm_package_runtime "$f")
                local pkgVersion=$(__dnvm_package_version "$f")

                local alias=""
                local delim=""
                for i in "${arr[@]}"; do
                    temp="$_DNVM_RUNTIME_PACKAGE_NAME-$pkgName.$pkgVersion"
                    temp2="$_DNVM_RUNTIME_PACKAGE_NAME-$pkgName-x86.$pkgVersion"
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
$_DNVM_COMMAND_NAME list default >/dev/null && $_DNVM_COMMAND_NAME use default >/dev/null || true
