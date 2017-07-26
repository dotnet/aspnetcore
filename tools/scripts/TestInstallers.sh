#!/usr/bin/env bash
set -e
set -o pipefail

artifactsDir="artifacts/"
installersDir=$artifactsDir"installers/"
dotnetInstallersDir=$artifactsDir"dotnetInstallers/"
installerTestDir=$artifactsDir"installerTest/"

# clean up test directory
if [ -d $installerTestDir ]; then
  rm -r $installerTestDir
fi
mkdir $installerTestDir

# copy dotnet installers
cp $dotnetInstallersDir*$DotnetInstallerSuffix $installerTestDir

# copy store and hosting installers
cp $installersDir$StoreInstallerFile $installerTestDir
cp $installersDir$HostingInstallerFile $installerTestDir

# test store install
(cd $installerTestDir && $InstallCommand $StoreInstallerFile)

if [ ! -d $InstallRoot"additionalDeps" ]; then
  echo $InstallRoot"additionalDeps missing after installing the runtime store."
  exit 1
fi
if [ ! -d $InstallRoot"store" ]; then
  echo $InstallRoot"store missing after installing the runtime store."
  exit 1
fi

# test store uninstall
$UninstallCommand $StoreInstallerPackageName

if [ -d $InstallRoot"additionalDeps" ]; then
  echo $InstallRoot"additionalDeps remains after uninstalling the runtime store."
  exit 1
fi
if [ -d $InstallRoot"store" ]; then
  echo $InstallRoot"store remains after uninstalling the runtime store."
  exit 1
fi

# test hosting install
(cd $installerTestDir && $InstallCommand *$InstallerExtension)

if [ ! -d $InstallRoot"additionalDeps" ]; then
  echo $InstallRoot"additionalDeps missing after installing the hosting bundle."
  exit 1
fi
if [ ! -d $InstallRoot"shared" ]; then
  echo $InstallRoot"shared missing after installing the hosting bundle."
  exit 1
fi
if [ ! -d $InstallRoot"store" ]; then
  echo $InstallRoot"store missing after installing the hosting bundle."
  exit 1
fi

# test hosting uninstall
$UninstallCommand $HostingInstallerPackageName

if [ ! -d $InstallRoot"additionalDeps" ]; then
  echo $InstallRoot"additionalDeps missing after installing the hosting bundle."
  exit 1
fi
if [ ! -d $InstallRoot"shared" ]; then
  echo $InstallRoot"shared missing after installing the hosting bundle."
  exit 1
fi
if [ ! -d $InstallRoot"store" ]; then
  echo $InstallRoot"store missing after installing the hosting bundle."
  exit 1
fi
