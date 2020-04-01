#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

current_user=$(whoami)
if [ $current_user != "root" ]; then
    echo "test.sh requires superuser privileges to run"
    exit 1
fi

run_unit_tests(){
    bats $DIR/test/unit_tests/test_debian_build_lib.bats
    bats $DIR/test/unit_tests/test_scripts.bats
}

run_integration_tests(){
    input_dir=$DIR/test/test_assets/test_package_layout
    output_dir=$DIR/bin

    # Create output dir
    mkdir -p $output_dir

    # Build the actual package
    $DIR/package_tool -i $input_dir -o $output_dir

    # Integration Test Entrypoint placed by package_tool
    bats $output_dir/test_package.bats

    # Cleanup output dir
    rm -rf $DIR/test/test_assets/test_package_output
}

run_all(){
    run_unit_tests
    run_integration_tests
}

run_all
