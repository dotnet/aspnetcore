#!/bin/bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# This script is used to test the debian package after it's creation.
# The package tool will drop it next to the .deb file it creates.
# Environment Variables:
#   LAST_VERSION_URL: Url for last version .deb (to test upgrades) [required for upgrade test]

#Ensure running with superuser privileges
current_user=$(whoami)
if [ $current_user != "root" ]; then
	echo "WARNING: test_package.bats requires superuser privileges to run, trying sudo..."
	SUDO_PREFIX="sudo"
fi

setup(){
	DIR="$BATS_TEST_DIRNAME"

	PACKAGE_FILENAME="$(ls $DIR | grep .deb -m 1)"
	PACKAGE_PATH="$DIR/*.deb"

	# Get Package name from package path, 
	PACKAGE_NAME=${PACKAGE_FILENAME%%_*}
}

install_package(){
	$SUDO_PREFIX dpkg -i $PACKAGE_PATH
}

remove_package(){
	$SUDO_PREFIX dpkg -r $PACKAGE_NAME
}

purge_package(){
	$SUDO_PREFIX dpkg -P $PACKAGE_NAME
}

install_last_version(){
	$SUDO_PREFIX dpkg -i "$DIR/last_version.deb"
}

download_and_install_last_version(){
	curl "$LAST_VERSION_URL" -o "$DIR/last_version.deb"
	
	install_last_version
}

delete_last_version(){
        rm -f "$DIR/last_version.deb"
}

teardown(){
        delete_last_version
}

@test "package install + removal test" {
	install_package
	remove_package
}

@test "package install + purge test" {
	install_package
	purge_package
}

# Ultimate Package Test
# https://www.debian.org/doc/manuals/maint-guide/checkit.en.html#pmaintscripts
@test "package install + upgrade + purge + install + remove + install + purge test" {
	if [ ! -z "$LAST_VERSION_URL" ]; then
		download_and_install_last_version
		install_package
		purge_package
		install_package
		remove_package
		install_package
		purge_package
	fi
}
