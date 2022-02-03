#!/usr/bin/env bash

brew update
brew list openssl || brew install openssl
brew list nginx || brew install nginx
