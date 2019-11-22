#!/usr/bin/env bash

set -eo pipefail

RED="\033[0;31m"
RESET="\033[0m"

if [ -z "$1" ]; then
  echo -e "${RED}The JDK version command-line parameter is required.${RESET}"
  exit 1
fi

if [ -z "$JAVA_HOME" ]; then
  echo -e "${RED}The JAVA_HOME environment variable must be set before using this command.${RESET}"
  exit 2
fi

failed=false
repoRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/../.."
tempDir="$repoRoot/obj"

jdkVersion="$1"
zipName="jdk-${jdkVersion}_linux-x64_bin.tar.gz"

echo "Starting download of JDK $jdkVersion."
mkdir -p "$tempDir"
cd "$tempDir"
rm --force "$zipName"
curl --fail --location --remote-name --retry 10 --show-error --silent \
  "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/linux/java/$zipName" || \
  failed=true

if [ "$failed" = true ]; then
  echo -e "${RED}JDK $jdkVersion download failed.${RESET}"
  exit 3
fi

echo "Starting expansion of JDK $jdkVersion to $tempDir."
rm --force --recursive "jdk-${jdkVersion}"
tar --extract --file="$zipName" --gunzip || \
  failed=true

if [ "$failed" = true ]; then
  echo -e "${RED}JDK $jdkVersion expansion failed.${RESET}"
  exit 4
fi

echo "Installing JDK to $JAVA_HOME"
rm --force --recursive "$JAVA_HOME"
mkdir -p "$JAVA_HOME/.."
mv --force --no-target-directory "jdk-${jdkVersion}" "$JAVA_HOME"

echo "Done installing JDK $jdkVersion to $JAVA_HOME"
