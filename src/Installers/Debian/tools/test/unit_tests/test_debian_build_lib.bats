#!/usr/bin/env bats
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Tests for debian_build_lib.sh

setup(){
    DIR="$BATS_TEST_DIRNAME"
    PACKAGIFY_DIR="$(readlink -f $DIR/../../)"

    PACKAGE_DIR="$BATS_TMPDIR/test-package"
    PACKAGE_SOURCE_DIR="$BATS_TMPDIR/test-source-package"
    INSTALL_ROOT="test-install-root"

    # # Create Mock Package Directory
    mkdir -p $PACKAGE_SOURCE_DIR/debian
    mkdir $PACKAGE_DIR

    source $PACKAGIFY_DIR/scripts/debian_build_lib.sh

}

teardown(){
    # Remove Mock Package Directory
    rm -r $PACKAGE_DIR
    rm -r $PACKAGE_SOURCE_DIR
}

@test "add_system_file_placement populates placement array" {
    
    add_system_file_placement "testfile0" "testdir0"
    add_system_file_placement "testfile1" "testdir1"

    [ $placement_index -eq "2" ]
    [ "${install_placement[0]}" = "testfile0 testdir0" ]
    [ "${install_placement[1]}" = "testfile1 testdir1" ]
}

@test "add_system_dir_placement adds files in dir to placement array" {
    test_package_rel_dir="test-dir"
    abs_test_path="/abs/test/path"

    mkdir $PACKAGE_SOURCE_DIR/$test_package_rel_dir
    echo "file0 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/file0
    echo "file1 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/file1
    echo "file2 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/file2

    add_system_dir_placement $test_package_rel_dir $abs_test_path

    rm -r $PACKAGE_SOURCE_DIR/$test_package_rel_dir

    [ "$placement_index" -eq 3 ]
    [ "${install_placement[0]}" = "$test_package_rel_dir/file0 $abs_test_path" ]
    [ "${install_placement[1]}" = "$test_package_rel_dir/file1 $abs_test_path" ]
    [ "${install_placement[2]}" = "$test_package_rel_dir/file2 $abs_test_path" ]
}

@test "add_system_dir_placement adds all files in subdir to placement array" {
    test_package_rel_dir="test-dir"
    test_package_rel_subdir="test-subdir"
    abs_test_path="/abs/test/path"

    mkdir -p $PACKAGE_SOURCE_DIR/$test_package_rel_dir/$test_package_rel_subdir
    echo "file0 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/$test_package_rel_subdir/file0
    echo "file1 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/$test_package_rel_subdir/file1
    echo "file2 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/$test_package_rel_subdir/file2

    add_system_dir_placement $test_package_rel_dir $abs_test_path

    rm -r $PACKAGE_SOURCE_DIR/$test_package_rel_dir

    [ "$placement_index" -eq "3" ]
    [ "${install_placement[0]}" = "$test_package_rel_dir/$test_package_rel_subdir/file0 $abs_test_path/$test_package_rel_subdir" ]
    [ "${install_placement[1]}" = "$test_package_rel_dir/$test_package_rel_subdir/file1 $abs_test_path/$test_package_rel_subdir" ]
    [ "${install_placement[2]}" = "$test_package_rel_dir/$test_package_rel_subdir/file2 $abs_test_path/$test_package_rel_subdir" ]
}

@test "add_system_dir_placement adds all files in subdir and dir to placement array" {
    test_package_rel_dir="test-dir"
    test_package_rel_subdir="test-subdir"
    abs_test_path="/abs/test/path"

    mkdir -p $PACKAGE_SOURCE_DIR/$test_package_rel_dir/$test_package_rel_subdir
    echo "file0 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/file0
    echo "file1 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/file1
    echo "file2 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/file2
    echo "file3 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/$test_package_rel_subdir/file3
    echo "file4 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/$test_package_rel_subdir/file4
    echo "file5 contents" > $PACKAGE_SOURCE_DIR/$test_package_rel_dir/$test_package_rel_subdir/file5

    add_system_dir_placement $test_package_rel_dir $abs_test_path

    rm -r $PACKAGE_SOURCE_DIR/$test_package_rel_dir

    [ "$placement_index" -eq "6" ]
    [ "${install_placement[0]}" = "$test_package_rel_dir/file0 $abs_test_path" ]
    [ "${install_placement[1]}" = "$test_package_rel_dir/file1 $abs_test_path" ]
    [ "${install_placement[2]}" = "$test_package_rel_dir/file2 $abs_test_path" ]
    [ "${install_placement[3]}" = "$test_package_rel_dir/$test_package_rel_subdir/file3 $abs_test_path/$test_package_rel_subdir" ]
    [ "${install_placement[4]}" = "$test_package_rel_dir/$test_package_rel_subdir/file4 $abs_test_path/$test_package_rel_subdir" ]
    [ "${install_placement[5]}" = "$test_package_rel_dir/$test_package_rel_subdir/file5 $abs_test_path/$test_package_rel_subdir" ]
}

@test "write_debian_install_file writes to debian/install" {
    add_system_file_placement "somefile" "/some/abs/path"
    write_debian_install_file

    [ -f "$PACKAGE_SOURCE_DIR/debian/install" ]
    [ "$(cat $PACKAGE_SOURCE_DIR/debian/install)" = "somefile /some/abs/path" ]
}

@test "add_file_to_install adds file to package_dest_dir" {
    local_test_dir="$BATS_TMPDIR/local-dir"
    local_subdir="the/path/should/not/matter"
    package_test_dir="package-dir"

    mkdir $PACKAGE_SOURCE_DIR/$package_test_dir
    mkdir -p $local_test_dir/$local_subdir
    echo "file0 contents" > $local_test_dir/$local_subdir/file0
    echo "file1 contents" > $local_test_dir/$local_subdir/file1
    echo "file2 contents" > $local_test_dir/$local_subdir/file2

    add_file_to_install "$local_test_dir/$local_subdir/file0" "$package_test_dir"
    add_file_to_install "$local_test_dir/$local_subdir/file1" "$package_test_dir"
    add_file_to_install "$local_test_dir/$local_subdir/file2" "$package_test_dir"

    rm -r $local_test_dir

    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file0" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file1" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file2" ]
}

@test "add_file_to_install added files written to install file after write_debian_install_file" {
    local_test_dir="$BATS_TMPDIR/local-dir"
    local_subdir="the/path/should/not/matter"
    package_test_dir="package-dir"

    mkdir $PACKAGE_SOURCE_DIR/$package_test_dir
    mkdir -p $local_test_dir/$local_subdir
    echo "file0 contents" > $local_test_dir/$local_subdir/file0

    add_file_to_install "$local_test_dir/$local_subdir/file0" "$package_test_dir"

    rm -r $local_test_dir

    [ ! -e $PACKAGE_SOURCE_DIR/debian/install ]
    write_debian_install_file
    [ -f $PACKAGE_SOURCE_DIR/debian/install ]
    [ "$(cat $PACKAGE_SOURCE_DIR/debian/install)" = "$package_test_dir/file0 $INSTALL_ROOT/$package_test_dir" ]
}

@test "add_dir_to_install adds files in dir to package_dest_dir" {
    local_test_dir="$BATS_TMPDIR/local-dir"
    package_test_dir="package-dir"

    mkdir $PACKAGE_SOURCE_DIR/$package_test_dir
    mkdir $local_test_dir
    echo "file0 contents" > $local_test_dir/file0
    echo "file1 contents" > $local_test_dir/file1
    echo "file2 contents" > $local_test_dir/file2

    add_dir_to_install "$local_test_dir" "$package_test_dir"

    rm -r $local_test_dir

    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file0" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file1" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file2" ]
}

@test "add_dir_to_install adds files in subdirectory tree to package_dest_dir" {
    local_test_dir="$BATS_TMPDIR/local-dir"
    local_test_subdir="local-subdir"

    package_test_dir="package-dir"

    mkdir $PACKAGE_SOURCE_DIR/$package_test_dir
    mkdir -p $local_test_dir/$local_test_subdir
    echo "file0 contents" > $local_test_dir/$local_test_subdir/file0
    echo "file1 contents" > $local_test_dir/$local_test_subdir/file1
    echo "file2 contents" > $local_test_dir/$local_test_subdir/file2

    add_dir_to_install "$local_test_dir" "$package_test_dir"

    rm -r $local_test_dir

    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/$local_test_subdir/file0" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/$local_test_subdir/file1" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/$local_test_subdir/file2" ]
}

@test "add_dir_to_install adds files in directory and subdirectory tree to package_dest_dir" {
    local_test_dir="$BATS_TMPDIR/local-dir"
    local_test_subdir="local-subdir"

    package_test_dir="package-dir"

    mkdir $PACKAGE_SOURCE_DIR/$package_test_dir
    mkdir -p $local_test_dir/$local_test_subdir

    echo "file0 contents" > $local_test_dir/file0
    echo "file1 contents" > $local_test_dir/file1
    echo "file2 contents" > $local_test_dir/file2
    echo "file3 contents" > $local_test_dir/$local_test_subdir/file3
    echo "file4 contents" > $local_test_dir/$local_test_subdir/file4
    echo "file5 contents" > $local_test_dir/$local_test_subdir/file5

    add_dir_to_install "$local_test_dir" "$package_test_dir"

    rm -r $local_test_dir

    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file0" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file1" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/file2" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/$local_test_subdir/file3" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/$local_test_subdir/file4" ]
    [ -f "$PACKAGE_SOURCE_DIR/$package_test_dir/$local_test_subdir/file5" ]
}

@test "add_dir_to_install added files written to install file after write_debian_install_file" {
    local_test_dir="$BATS_TMPDIR/local-dir"
    package_test_dir="package-dir"

    mkdir $PACKAGE_SOURCE_DIR/$package_test_dir
    mkdir -p $local_test_dir

    echo "file0 contents" > $local_test_dir/file0

    add_dir_to_install "$local_test_dir" "$package_test_dir"

    rm -r $local_test_dir

    [ ! -e $PACKAGE_SOURCE_DIR/debian/install ]
    write_debian_install_file
    [ -f $PACKAGE_SOURCE_DIR/debian/install ]
    [ "$(cat $PACKAGE_SOURCE_DIR/debian/install)" = "$package_test_dir/file0 $INSTALL_ROOT/$package_test_dir" ]
}

@test "add_dir_to_install with empty dest dir outputs to PACKAGE_SOURCE_DIR" {
    local_test_dir="$BATS_TMPDIR/local-dir"
    mkdir -p $local_test_dir

    echo "file0 contents" > $local_test_dir/file0

    add_dir_to_install "$local_test_dir" ""

    rm -r $local_test_dir

    [ -f "$PACKAGE_SOURCE_DIR/file0" ]
}


@test "add_dir_to_install with empty dest dir adds files in directory and subdirectory tree to $PACKAGE_SOURCE_DIR" {
    local_test_dir="$BATS_TMPDIR/local-dir"
    local_test_subdir="local-subdir"

    mkdir -p $local_test_dir/$local_test_subdir

    echo "file0 contents" > $local_test_dir/file0
    echo "file1 contents" > $local_test_dir/file1
    echo "file2 contents" > $local_test_dir/file2
    echo "file3 contents" > $local_test_dir/$local_test_subdir/file3
    echo "file4 contents" > $local_test_dir/$local_test_subdir/file4
    echo "file5 contents" > $local_test_dir/$local_test_subdir/file5

    add_dir_to_install "$local_test_dir" ""

    rm -r $local_test_dir

    [ -f "$PACKAGE_SOURCE_DIR/file0" ]
    [ -f "$PACKAGE_SOURCE_DIR/file1" ]
    [ -f "$PACKAGE_SOURCE_DIR/file2" ]
    [ -f "$PACKAGE_SOURCE_DIR/$local_test_subdir/file3" ]
    [ -f "$PACKAGE_SOURCE_DIR/$local_test_subdir/file4" ]
    [ -f "$PACKAGE_SOURCE_DIR/$local_test_subdir/file5" ]
}
