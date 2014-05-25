#!/bin/bash

set -e

_kvmsetup_has() {
  type "$1" > /dev/null 2>&1
  return $?
}

if [ -z "$KRE_USER_HOME" ]; then
  eval KRE_USER_HOME=~/.kre
fi

if ! _kvmsetup_has "curl"; then
  echo "kvmsetup requires curl to be installed"
  return 1
fi

if [ -z "$KVM_SOURCE" ]; then
  KVM_SOURCE="https://raw.githubusercontent.com/graemechristie/Home/KvmShellImplementation/kvm.sh"
fi

# Downloading to $KVM_DIR
mkdir -p "$KRE_USER_HOME/kvm"
if [ -s "$KRE_USER_HOME/kvm/kvm.sh" ]; then
  echo "=> kvm is already installed in $KRE_USER_HOME/kvm, trying to update"
else
  echo "=> Downloading kvm as script to '$KRE_USER_HOME/kvm'"
fi

curl -s "$KVM_SOURCE" -o "$KRE_USER_HOME/kvm/kvm.sh" || {
  echo >&2 "Failed to download '$KVM_SOURCE'.."
  return 1
}


echo

# Detect profile file if not specified as environment variable (eg: PROFILE=~/.myprofile).
if [ -z "$PROFILE" ]; then
  if [ -f "$HOME/.bash_profile" ]; then
    PROFILE="$HOME/.bash_profile"
  elif [ -f "$HOME/.zshrc" ]; then
    PROFILE="$HOME/.zshrc"
  elif [ -f "$HOME/.profile" ]; then
    PROFILE="$HOME/.profile"
  fi
fi

SOURCE_STR="[ -s \"$KRE_USER_HOME/kvm/kvm.sh\" ] && . \"$KRE_USER_HOME/kvm/kvm.sh\"# this loads kvm"

if [ -z "$PROFILE" ] || [ ! -f "$PROFILE" ] ; then
  if [ -z $PROFILE ]; then
    echo "=> Profile not found. Tried ~/.bash_profile ~/.zshrc and ~/.profile."
    echo "=> Create one of them and run this script again"
  else
    echo "=> Profile $PROFILE not found"
    echo "=> Create it (touch $PROFILE) and run this script again"
  fi
  echo "   OR"
  echo "=> Append the following line to the correct file yourself:"
  echo
  echo "   $SOURCE_STR"
  echo
else
  if ! grep -qc 'kvm.sh' $PROFILE; then
    echo "=> Appending source string to $PROFILE"
    echo "" >> "$PROFILE"
    echo $SOURCE_STR >> "$PROFILE"
  else
    echo "=> Source string already in $PROFILE"
  fi
fi

echo "=> Close and reopen your terminal to start using kvm"
echo "=> then type \"kvm upgrade\" to install the latest version of the K Runtime Environment"
