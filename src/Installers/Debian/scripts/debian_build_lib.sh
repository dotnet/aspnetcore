#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# This file is not intended to be executed directly
# Import these functions using source
#
# Relies on these environment variables:
# PACKAGE_SOURCE_DIR :: Package Source Staging Directory
# INSTALL_ROOT :: Absolute path of package installation root

# write_debian_install_file
# Summary: Writes the contents of the "install_placement" array to the debian/install
#   This array is populated by calls to the "add_system_file_placement" function
# Usage: write_debian_install_file
write_debian_install_file(){
    # Remove any existing install file, we need to overwrite it
    rm -f ${PACKAGE_SOURCE_DIR}/debian/install

    for i in "${install_placement[@]}"
    do
        echo "${i}" >> "${PACKAGE_SOURCE_DIR}/debian/install"
    done
}

# add_system_file_placement
# Summary:  Registers a file placement on the filesystem from the package by populating the "install_placement" array
# Usage: add_system_file_placement {local path of file in package} {absolute path of directory to place file in}
add_system_file_placement(){
    #Initialize placement_index variable
    if [[ -z "$placement_index" ]]; then
        placement_index=0
    fi

    install_placement[${placement_index}]="${1} ${2}"
    placement_index=$((${placement_index}+1))
}

# add_system_dir_placement
# Summary: Registers a directory placement on the post-installation package from an in-package path
add_system_dir_placement(){

    in_package_dir=$1
    abs_installation_dir=$2

    dir_files=( $(_get_files_in_dir_tree $PACKAGE_SOURCE_DIR/$in_package_dir) )
    
    # If in_package_dir isn't empty include a slash
    if [ ! -z "$in_package_dir" ]; then
        in_package_dir="${in_package_dir}/"
    fi

    for rel_filepath in ${dir_files[@]}
    do
        local parent_path=$(dirname $rel_filepath)

        # If there is no parent, parent_path = "."
        if [[ "$parent_path" == "." ]]; then
            add_system_file_placement "${in_package_dir}${rel_filepath}" "${abs_installation_dir}"
        else
            add_system_file_placement "${in_package_dir}${rel_filepath}" "${abs_installation_dir}/${parent_path}"
        fi

    done
}

# add_file_to_install
# Summary: Adds a file from the local filesystem to the package and installs it rooted at INSTALL_ROOT
# Usage: add_install_file {relative path to local file} {relative path to INSTALL_ROOT to place file}
add_file_to_install(){
    copy_from_file=$1
    rel_install_path=$2

    local filename=$(basename $copy_from_file)
    local parent_dir=$(dirname $copy_from_file)

    # Create Relative Copy From Path
    rel_copy_from_file=${copy_from_file#$parent_dir/}

    # Delete any existing file and ensure path exists
    rm -f ./${PACKAGE_SOURCE_DIR}/${rel_install_path}/${filename}
    mkdir -p ./${PACKAGE_SOURCE_DIR}/${rel_install_path}

    dir_files=( "$rel_copy_from_file" )

    _copy_files_to_package $parent_dir $rel_install_path "${dir_files[@]}"

    add_system_file_placement "${rel_install_path}/${filename}" "${INSTALL_ROOT}/$rel_install_path"
}

# add_dir_to_install
# Summary: Adds contents of a directory on the local filesystem to the package and installs them rooted at INSTALL_ROOT
#     Note: Does not install the directory passed, only its contents 
# Usage: add_dir_to_install {relative path of directory to copy} {relative path to INSTALL_ROOT to place directory tree}
add_dir_to_install(){
    
    copy_from_dir=$1
    rel_install_path=$2

    # Delete and Create any existing directory
    mkdir -p ${PACKAGE_SOURCE_DIR}/${rel_install_path}

    dir_files=( $(_get_files_in_dir_tree $copy_from_dir) )

    _copy_files_to_package "$copy_from_dir" "$rel_install_path" "${dir_files[@]}"

    for file in "${dir_files[@]}"
    do
        file_rel_dir="$(dirname $file)"
        add_system_file_placement "${rel_install_path}/${file}" "${INSTALL_ROOT}/$rel_install_path/${file_rel_dir}"
    done
}

# Usage: _copy_files_to_package {local files root directory} {relative directory in package to copy to} "${filepath_array[@]}"
# Note: The specific syntax on the parameter shows how to pass an array
_copy_files_to_package(){
    local_root_dir=$1
    package_dest_dir=$2

    # Consume the remaining input as an array
    shift; shift;
    rel_filepath_list=( $@ )

    for rel_filepath in ${rel_filepath_list[@]}
    do
        local parent_dir=$(dirname $rel_filepath)
        local filename=$(basename $rel_filepath)
            
        mkdir -p ${PACKAGE_SOURCE_DIR}/${package_dest_dir}/${parent_dir}

        # Ignore $parent_dir if it there isn't one
        if [[ "$parent_dir" == "." ]]; then
            cp "${local_root_dir}/${rel_filepath}" "${PACKAGE_SOURCE_DIR}/${package_dest_dir}"
        else
            cp "${local_root_dir}/${rel_filepath}" "${PACKAGE_SOURCE_DIR}/${package_dest_dir}/${parent_dir}"
        fi
        
    done
}

# Usage: _get_files_in_dir_tree {path of directory}
_get_files_in_dir_tree(){

    root_dir=$1

    # Use Globstar expansion to enumerate all directories and files in the tree
    shopt -s globstar
    shopt -s dotglob
    dir_tree_list=( "${root_dir}/"** )

    # Build a new array with only the Files contained in $dir_tree_list
    local index=0
    for file_path in "${dir_tree_list[@]}"
    do
        if [ -f $file_path ]; then
            dir_tree_file_list[${index}]=$file_path
            index=$(($index+1))
        fi
    done

    # Remove $root_dir prefix from each path in dir_tree_file_list
    # This is confusing syntax, so here's a reference link (Substring Removal)
    #     http://wiki.bash-hackers.org/syntax/pe
    dir_tree_file_list=( "${dir_tree_file_list[@]#${root_dir}/}" )

    # Echo is the return mechanism
    echo "${dir_tree_file_list[@]}"
}

