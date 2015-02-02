#!/bin/bash

[ -z "$KVM_REPO" ] && KVM_REPO="aspnet/Home"
[ -z "$KVM_BRANCH" ] && KVM_BRANCH="master"

_kvmsetup_has() {
    type "$1" > /dev/null 2>&1
    return $?
}

_kvmsetup_update_profile() {
    local profile="$1"
    local sourceString="$2"
    if ! grep -qc 'kvm.sh' $profile; then
        echo "Appending source string to $profile"
        echo "" >> "$profile"
        echo $sourceString >> "$profile"
    else
        echo "=> Source string already in $profile"
    fi
}

if [ -z "$KRE_USER_HOME" ]; then
    eval KRE_USER_HOME=~/.k
fi

if ! _kvmsetup_has "curl"; then
    echo "kvmsetup requires curl to be installed"
    return 1
fi

if [ -z "$KVM_SOURCE" ]; then
    KVM_SOURCE="https://raw.githubusercontent.com/$KVM_REPO/$KVM_BRANCH/kvm.sh"
fi

# Downloading to $KVM_DIR
mkdir -p "$KRE_USER_HOME/kvm"
if [ -s "$KRE_USER_HOME/kvm/kvm.sh" ]; then
    echo "kvm is already installed in $KRE_USER_HOME/kvm, trying to update"
else
    echo "Downloading kvm as script to '$KRE_USER_HOME/kvm'"
fi

echo "Downloading kvm from '$KVM_SOURCE'"

curl -s "$KVM_SOURCE" -o "$KRE_USER_HOME/kvm/kvm.sh" || {
    echo >&2 "Failed to download '$KVM_SOURCE'.."
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

SOURCE_STR="[ -s \"$KRE_USER_HOME/kvm/kvm.sh\" ] && . \"$KRE_USER_HOME/kvm/kvm.sh\" # Load kvm"

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
    [ -n "$PROFILE" ] && _kvmsetup_update_profile "$PROFILE" "$SOURCE_STR"
    [ -n "$ZPROFILE" ] && _kvmsetup_update_profile "$ZPROFILE" "$SOURCE_STR"
fi

echo "Type 'source $KRE_USER_HOME/kvm/kvm.sh' to start using kvm"
