#!/bin/bash

_dotnetsdksetup_has() {
    type "$1" > /dev/null 2>&1
    return $?
}

_dotnetsdksetup_update_profile() {
    local profile="$1"
    local sourceString="$2"
    if ! grep -qc 'dotnetsdk.sh' $profile; then
        echo "Appending source string to $profile"
        echo "" >> "$profile"
        echo $sourceString >> "$profile"
    else
        echo "=> Source string already in $profile"
    fi
}

if [ -z "$DOTNET_USER_HOME" ]; then
    eval DOTNET_USER_HOME=~/.dotnet
fi

DEFAULT_INSTALL_PATH="$DOTNET_USER_HOME/bin/dotnetsdk.sh"

if ! _dotnetsdksetup_has "curl"; then
    echo "dotnetsdksetup requires curl to be installed"
    return 1
fi

if [ -z "$DOTNETSDK_SOURCE" ]; then
    DOTNETSDK_SOURCE="https://raw.githubusercontent.com/aspnet/Home/master/dotnetsdk.sh"
fi

# Downloading to $KVM_DIR
mkdir -p "$DOTNET_USER_HOME/bin"
if [ -s "$DEFAULT_INSTALL_PATH" ]; then
    echo "dotnetsdk is already installed in $DOTNET_USER_HOME/dotnetsdk, trying to update"
else
    echo "Downloading dotnetsdk as script to '$DOTNET_USER_HOME/dotnetsdk'"
fi

curl -s "$DOTNETSDK_SOURCE" -o "$DOTNET_USER_HOME/dotnetsdk/dotnetsdk.sh" || {
    echo >&2 "Failed to download dotnetsdk from '$DOTNETSDK_SOURCE'.."
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

SOURCE_STR="[ -s \"$DOTNET_USER_HOME/dotnetsdk/dotnetsdk.sh\" ] && . \"$DOTNET_USER_HOME/dotnetsdk/dotnetsdk.sh\" # Load dotnetsdk"

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
    [ -n "$PROFILE" ] && _dotnetsdksetup_update_profile "$PROFILE" "$SOURCE_STR"
    [ -n "$ZPROFILE" ] && _dotnetsdksetup_update_profile "$ZPROFILE" "$SOURCE_STR"
fi

echo "Type 'source $DOTNET_USER_HOME/bin/dotnetsdk.sh' to start using dotnetsdk"
