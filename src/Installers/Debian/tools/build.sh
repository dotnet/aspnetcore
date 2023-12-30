#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

## Load Functions ##
source $SCRIPT_DIR/scripts/debian_build_lib.sh

## Debian Package Creation Functions ##
execute(){
    if ! parse_args_and_set_env_vars "$@"; then
        exit 1
    fi

    # Exit if required validation fails
    if ! validate_inputs; then
        exit 1
    fi

    parse_config_and_set_env_vars
    clean_or_create_build_dirs
    package_all
    generate_all
    create_source_tarball

    # Actually Build Package Files
    (cd ${PACKAGE_SOURCE_DIR}; debuild -us -uc)

    copy_files_to_output
}

parse_args_and_set_env_vars(){
    OPTIND=1 # Reset in case getopts has been used previously in the shell.

    while getopts ":n:v:i:o:h:C:" opt; do
      case $opt in
        n)
          export PACKAGE_NAME="$OPTARG"
          ;;
        v)
          export PACKAGE_VERSION="$OPTARG"
          ;;
        i)
          export INPUT_DIR="$OPTARG"
          ;;
        C)
          export CONTENT_DIR="$OPTARG"
          ;;
        o)
          export OUTPUT_DIR="$OPTARG"
          ;;
        h)
          print_help
          return 1
          ;;
        \?)
          echo "Invalid option: -$OPTARG" >&2
          return 1
          ;;
        :)
          echo "Option -$OPTARG requires an argument." >&2
          return 1
          ;;
      esac
    done

     # Special Input Directories + Paths
    ABSOLUTE_PLACEMENT_DIR="${INPUT_DIR}/\$"
    PACKAGE_ROOT_PLACEMENT_DIR="${CONTENT_DIR}"
    CONFIG="$INPUT_DIR/debian_config.json"

    return 0
}

print_help(){
    echo "Usage: package_tool [-i <INPUT_DIR>] [-o <OUTPUT_DIRECTORY>]
    [-n <PACKAGE_NAME>] [-v <PACKAGE_VERSION>] [-h]

    REQUIRED:
        -i <INPUT_DIR>: Input directory conforming to package_tool conventions and debian_config.json
        -C <CONTENT_DIR>: Directory containing the files which should be packaged.
        -o <OUTPUT_DIR>: Output directory for debian package and other artifacts

    OPTIONAL:
        -n <PACKAGE_NAME>: name of created package, will override value in debian_config.json
        -v <PACKAGE_VERSION>: version of created package, will override value in debian_config.json
        -h: Show this message

    NOTES:
        See Readme for more information on package_tool conventions and debian_config.json format
        https://github.com/dotnet/cli/tree/master/packaging/debian/package_tool
    "
}

validate_inputs(){
    local ret=0
    if [[ -z "$INPUT_DIR" ]]; then
        echo "ERROR: -i <INPUT_DIRECTORY> Not Specified"
        ret=1
    fi

    if [[ -z "$OUTPUT_DIR" ]]; then
        echo "ERROR: -o <OUTPUT_DIRECTORY> Not Specified."
        ret=1
    fi

    if [[ -z "$CONTENT_DIR" ]]; then
        echo "ERROR: -C <CONTENT_DIR> Not Specified."
        ret=1
    elif [[ ! -d "$PACKAGE_ROOT_PLACEMENT_DIR" ]]; then
        echo "ERROR: '$PACKAGE_ROOT_PLACEMENT_DIR' directory does not exist"
        ret=1
    fi

    if [[ ! -f "$CONFIG" ]]; then
        echo "ERROR: debian_config.json file does not exist"
        echo $CONFIG
        ret=1
    fi

    return $ret
}

parse_config_and_set_env_vars(){
    extract_base_cmd="python3 $SCRIPT_DIR/scripts/extract_json_value.py"

    # Arguments Take Precedence over Config
    [ -z "$PACKAGE_VERSION" ] && PACKAGE_VERSION="$($extract_base_cmd $CONFIG "release.package_version")"
    [ -z "$PACKAGE_NAME" ] && PACKAGE_NAME="$($extract_base_cmd $CONFIG "package_name")"

    # Inputs
    INPUT_SAMPLES_DIR="$INPUT_DIR/samples"
    INPUT_DOCS_DIR="$INPUT_DIR/docs"
    DOCS_JSON_PATH="$INPUT_DIR/docs.json"

    PACKAGE_SOURCE_DIR="${OUTPUT_DIR}/${PACKAGE_NAME}-${PACKAGE_VERSION}"

    if ! INSTALL_ROOT="$($extract_base_cmd $CONFIG "install_root")"; then
        INSTALL_ROOT="/usr/share/$PACKAGE_NAME"
    fi

    DEBIAN_DIR="${PACKAGE_SOURCE_DIR}/debian"
    DOCS_DIR="${PACKAGE_SOURCE_DIR}/docs"
}

clean_or_create_build_dirs(){
    rm -rf ${PACKAGE_SOURCE_DIR}
    mkdir -p $DEBIAN_DIR
}

package_all(){
    package_static_files
    package_package_root_placement
    package_absolute_placement
    package_samples
    package_docs
    package_install_scripts
}

generate_all(){
    generate_config_templates
    generate_manpages
    generate_manpage_manifest
    generate_sample_manifest
    write_debian_install_file
}

create_source_tarball(){
    rm -f ${OUTPUT_DIR}/${PACKAGE_NAME}_${PACKAGE_VERSION}.orig.tar.gz
    tar -cvzf ${OUTPUT_DIR}/${PACKAGE_NAME}_${PACKAGE_VERSION}.orig.tar.gz -C $PACKAGE_SOURCE_DIR .
}

copy_files_to_output(){
    # .deb, .dsc, etc.. Already in output dir
    # Copy Test files

    cp $SCRIPT_DIR/test/integration_tests/test_package.bats $OUTPUT_DIR
}

## Packaging Functions ##
package_static_files(){
    cp -a $SCRIPT_DIR/package_files/debian/* ${PACKAGE_SOURCE_DIR}/debian
}

package_package_root_placement(){
    add_dir_to_install ${PACKAGE_ROOT_PLACEMENT_DIR} ""
}

package_absolute_placement(){
    if [[ -d "$ABSOLUTE_PLACEMENT_DIR" ]]; then
        abs_in_package_dir="\$"

        add_dir_to_install ${ABSOLUTE_PLACEMENT_DIR} $abs_in_package_dir

        # Get List of all files in directory tree, relative to ABSOLUTE_PLACEMENT_DIR
        abs_files=( $(_get_files_in_dir_tree $ABSOLUTE_PLACEMENT_DIR) )

        # For each file add a a system placement
        for abs_file in ${abs_files[@]}
        do
            parent_dir=$(dirname $abs_file)
            filename=$(basename $abs_file)

            add_system_file_placement "$abs_in_package_dir/$abs_file" "/$parent_dir"
        done
    fi
}

package_samples(){
    if [[ -d "$INPUT_SAMPLES_DIR" ]]; then
        cp -a $INPUT_SAMPLES_DIR/. $PACKAGE_SOURCE_DIR
    fi
}

package_docs(){
    if [[ -d "$INPUT_DOCS_DIR" ]]; then
        mkdir -p $DOCS_DIR
        cp -a $INPUT_DOCS_DIR/. $DOCS_DIR
    fi
}

package_install_scripts(){
    # copy scripts for the package's control section like preinst, postint, etc
    if [[ -d "$INPUT_DIR/debian" ]]; then
        cp -a "$INPUT_DIR/debian/." $DEBIAN_DIR
    fi
}

## Generation Functions ##
generate_config_templates(){
    python3 ${SCRIPT_DIR}/scripts/config_template_generator.py $CONFIG $SCRIPT_DIR/templates/debian $DEBIAN_DIR $PACKAGE_NAME $PACKAGE_VERSION
}

generate_manpages(){
    if [[ -f "$DOCS_JSON_PATH" ]]; then
        mkdir -p $DOCS_DIR

        # Generate the manpages from json spec
        python3 ${SCRIPT_DIR}/scripts/manpage_generator.py ${DOCS_JSON_PATH} ${DOCS_DIR}
    fi
}

generate_manpage_manifest(){
    # Get a list of files generated relative to $DOCS_DIR
    generated_manpages=( $(_get_files_in_dir_tree $DOCS_DIR) )

    # Get path relative to $PACKAGE_SOURCE_DIR to prepend to each filename
    # This syntax is bash substring removal
    docs_rel_path=${DOCS_DIR#${PACKAGE_SOURCE_DIR}/}

    # Remove any existing manifest
    rm -f ${DEBIAN_DIR}/${PACKAGE_NAME}.manpages

    for manpage in ${generated_manpages[@]}
    do
        echo "${docs_rel_path}/${manpage}" >> "${DEBIAN_DIR}/${PACKAGE_NAME}.manpages"
    done
}

generate_sample_manifest(){
    if [[ -d "$INPUT_SAMPLES_DIR" ]]; then
        generated_manpages=( $(_get_files_in_dir_tree $INPUT_SAMPLES_DIR) )

        rm -f sample_manifest
        for sample in ${samples[@]}
        do
            echo "$sample" >> "${DEBIAN_DIR}/${PACKAGE_NAME}.examples"
        done
    else
        echo "Provide a 'samples' directory in INPUT_DIR to package samples"
    fi
}

execute "$@"
