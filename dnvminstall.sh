#!/usr/bin/env bash

_dnvmsetup_has() {
    type "$1" > /dev/null 2>&1
    return $?
}

_dnvmsetup_update_profile() {
    local profile="$1"
    local sourceString="$2"
    if ! grep -qc 'dnvm.sh' $profile; then
        echo "Appending source string to $profile"
        echo "" >> "$profile"
        echo $sourceString >> "$profile"
    else
        echo "=> Source string already in $profile"
    fi
}

if [ -z "$DNX_USER_HOME" ]; then
    eval DNX_USER_HOME=~/.dnx
fi

if ! _dnvmsetup_has "curl"; then
    echo "dnvmsetup requires curl to be installed"
    return 1
fi

if [ -z "$DNVM_SOURCE" ]; then
    DNVM_SOURCE="https://raw.githubusercontent.com/aspnet/Home/dev/dnvm.sh"
fi

# Downloading to $DNVM_DIR
mkdir -p "$DNX_USER_HOME/dnvm"
if [ -s "$DNX_USER_HOME/dnvm/dnvm.sh" ]; then
    echo "dnvm is already installed in $DNX_USER_HOME/dnvm, trying to update"
else
    echo "Downloading dnvm as script to '$DNX_USER_HOME/dnvm'"
fi

curl -s "$DNVM_SOURCE" -o "$DNX_USER_HOME/dnvm/dnvm.sh" || {
    echo >&2 "Failed to download '$DNVM_SOURCE'.."
    return 1
}

echo

# Detect profile file if not specified as environment variable (eg: PROFILE=~/.myprofile).
if [ -z "$PROFILE" ]; then
    if [ -f "$HOME/.bash_profile" ]; then
        PROFILE="$HOME/.bash_profile"
    elif [ -f "$HOME/.bashrc" ]; then
        PROFILE="$HOME/.bashrc"
    elif [ -f "$HOME/.profile" ]; then
        PROFILE="$HOME/.profile"
    fi
fi

if [ -z "$ZPROFILE" ]; then
    if [ -f "$HOME/.zshrc" ]; then
        ZPROFILE="$HOME/.zshrc"
    fi
fi

SOURCE_STR="[ -s \"$DNX_USER_HOME/dnvm/dnvm.sh\" ] && . \"$DNX_USER_HOME/dnvm/dnvm.sh\" # Load dnvm"

if [ -z "$PROFILE" -a -z "$ZPROFILE" ] || [ ! -f "$PROFILE" -a ! -f "$ZPROFILE" ] ; then
    if [ -z "$PROFILE" ]; then
      echo "Profile not found. Tried ~/.bash_profile ~/.zshrc and ~/.profile."
      echo "Create one of them and run this script again"
    elif [ ! -f "$PROFILE" ]; then
      echo "Profile $PROFILE not found"
      echo "Create it (touch $PROFILE) and run this script again"
    else
      echo "Profile $ZPROFILE not found"
      echo "Create it (touch $ZPROFILE) and run this script again"
    fi
    echo "  OR"
    echo "Append the following line to the correct file yourself:"
    echo
    echo " $SOURCE_STR"
    echo
else
    [ -n "$PROFILE" ] && _dnvmsetup_update_profile "$PROFILE" "$SOURCE_STR"
    [ -n "$ZPROFILE" ] && _dnvmsetup_update_profile "$ZPROFILE" "$SOURCE_STR"
fi

echo "Type 'source $DNX_USER_HOME/dnvm/dnvm.sh' to start using dnvm"
