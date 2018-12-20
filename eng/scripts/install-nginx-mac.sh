#!/usr/bin/env bash

brew update
brew install openssl nginx
ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/
ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/
export PATH="$PATH:$HOME/nginxinstall/sbin/"