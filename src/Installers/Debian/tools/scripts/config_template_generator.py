#!/usr/bin/python3
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Parses debian_config.json and generates appropriate templates
# Where optional defaults exist, they are defined in the template_dict
#     of the appropriate generation function

import os
import sys
import json
import datetime

FILE_CHANGELOG = 'changelog'
FILE_CONTROL = 'control'
FILE_COPYRIGHT = 'copyright'
FILE_SYMLINK_FORMAT = '{package_name}.links'
FILE_RULES = 'rules'

PACKAGE_ROOT_FORMAT = "usr/share/{package_name}"
CHANGELOG_DATE_FORMAT = "%a, %d %b %Y %H:%M:%S %z"

# UTC Timezone for Changelog date
class UTC(datetime.tzinfo):
    def utcoffset(self, dt):
        return datetime.timedelta(0)

    def tzname(self, dt):
        return "UTC"

    def dst(self, dt):
        return datetime.timedelta(0)

# Generation Functions
def generate_and_write_all(config_data, template_dir, output_dir, package_name=None, package_version=None):
    try:
        changelog_contents = generate_changelog(
            config_data, 
            template_dir, 
            package_name=package_name, 
            package_version=package_version)

        control_contents = generate_control(config_data, template_dir, package_name=package_name)
        copyright_contents = generate_copyright(config_data, template_dir)
        symlink_contents = generate_symlinks(config_data, package_name=package_name)
        rules_contents = generate_rules(config_data, template_dir)
    except Exception as exc:
      print(exc)
      help_and_exit("Error: Generation Failed, check your config file.")

    write_file(changelog_contents, output_dir, FILE_CHANGELOG)
    write_file(control_contents, output_dir, FILE_CONTROL)
    write_file(copyright_contents, output_dir, FILE_COPYRIGHT)
    write_file(rules_contents, output_dir, FILE_RULES)

    # Symlink File is optional
    if symlink_contents:
        symlink_filename = get_symlink_filename(config_data, package_name=package_name)
        write_file(symlink_contents, output_dir, symlink_filename)

    return

def generate_rules(config_data, template_dir):
    template = get_template(template_dir, FILE_RULES)

    ignored_dependency_packages = config_data.get("debian_ignored_dependencies", None)
    override_text = ""

    if ignored_dependency_packages != None:
        override_text = "override_dh_shlibdeps:\n\tdh_shlibdeps --dpkg-shlibdeps-params=\""

        for package in ignored_dependency_packages:
            override_text += "-x{0} ".format(package)

        override_text += "\""

    return template.format(overrides=override_text)

def generate_changelog(config_data, template_dir, package_version=None, package_name=None):
    template = get_template(template_dir, FILE_CHANGELOG)

    release_data = config_data["release"]
    
    # Allow for Version Override
    config_package_version = release_data["package_version"]
    package_version = package_version or config_package_version

    template_dict = dict(\
        PACKAGE_VERSION=package_version,
        PACKAGE_REVISION=release_data["package_revision"],
        CHANGELOG_MESSAGE=release_data["changelog_message"],
        URGENCY=release_data.get("urgency", "low"),

        PACKAGE_NAME=package_name or config_data["package_name"],
        MAINTAINER_NAME=config_data["maintainer_name"],
        MAINTAINER_EMAIL=config_data["maintainer_email"], 
        DATE=datetime.datetime.now(UTC()).strftime(CHANGELOG_DATE_FORMAT)
    )

    contents = template.format(**template_dict)

    return contents

def generate_control(config_data, template_dir, package_name=None):
    template = get_template(template_dir, FILE_CONTROL)

    dependency_data = config_data.get("debian_dependencies", None)
    dependency_str = get_dependendent_packages_string(dependency_data)

    conflict_data = config_data.get("package_conflicts", [])
    conflict_str = ', '.join(conflict_data)

    # Default to empty dict, so we don't explode on nested optional values
    control_data = config_data.get("control", dict())

    template_dict = dict(\
        SHORT_DESCRIPTION=config_data["short_description"],
        LONG_DESCRIPTION=config_data["long_description"],
        HOMEPAGE=config_data.get("homepage", ""),

        SECTION=control_data.get("section", "misc"),
        PRIORITY=control_data.get("priority", "low"),
        ARCH=control_data.get("architecture", "all"),

        DEPENDENT_PACKAGES=dependency_str,
        CONFLICT_PACKAGES=conflict_str,

        PACKAGE_NAME=package_name or config_data["package_name"],
        MAINTAINER_NAME=config_data["maintainer_name"],
        MAINTAINER_EMAIL=config_data["maintainer_email"]
    )

    contents = template.format(**template_dict)

    return contents

def generate_copyright(config_data, template_dir):
    template = get_template(template_dir, FILE_COPYRIGHT)

    license_data = config_data["license"]

    template_dict = dict(\
        COPYRIGHT_TEXT=config_data["copyright"],
        LICENSE_NAME=license_data["type"],
        LICENSE_TEXT=license_data["full_text"]
    )

    contents = template.format(**template_dict)

    return contents

def generate_symlinks(config_data, package_name=None):
    symlink_entries = []
    package_root_path = get_package_root(config_data, package_name=package_name)

    symlink_data = config_data.get("symlinks", dict())

    for package_rel_path, symlink_path in iter(symlink_data.items()):

        package_abs_path = os.path.join(package_root_path, package_rel_path)

        symlink_entries.append( '%s %s' % (package_abs_path, symlink_path) )

    return '\n'.join(symlink_entries)
    
# Helper Functions
def get_package_root(config_data, package_name=None):
    config_install_root = config_data.get("install_root", None)
    package_name = package_name or config_data["package_name"]

    return config_install_root or PACKAGE_ROOT_FORMAT.format(package_name=package_name)

def get_symlink_filename(config_data, package_name=None):
    package_name = package_name or config_data["package_name"]
    return FILE_SYMLINK_FORMAT.format(package_name=package_name)

def get_dependendent_packages_string(debian_dependency_data):
    if debian_dependency_data is None:
        return ""

    dependencies = []
        
    for debian_package_name in debian_dependency_data:
        dep_str = debian_package_name

        if debian_dependency_data[debian_package_name].get("package_version", None):
            debian_package_version = debian_dependency_data[debian_package_name].get("package_version")

            dep_str += " (>= %s)" % debian_package_version

        dependencies.append(dep_str)

    # Leading Comma is important here
    return ', ' + ', '.join(dependencies)


def load_json(json_path):
    json_data = None
    with open(json_path, 'r') as json_file:
        json_data = json.load(json_file)

    return json_data

def get_template(template_dir, name):
    path = os.path.join(template_dir, name)
    template_contents = None

    with open(path, 'r') as template_file:
        template_contents = template_file.read()

    return template_contents

def write_file(contents, output_dir, name):
    path = os.path.join(output_dir, name)

    with open(path, 'w') as out_file:
        out_file.write(contents)

    return

# Tool Functions
def help_and_exit(msg):
    print(msg)
    sys.exit(1)

def print_usage():
    print("Usage: config_template_generator.py [config file path] [template directory path] [output directory] (package name) (package version)")

def parse_and_validate_args():
    if len(sys.argv) < 4:
        print_usage()
        help_and_exit("Error: Invalid Arguments")

    config_path = sys.argv[1]
    template_dir = sys.argv[2]
    output_dir = sys.argv[3]
    name_override = None
    version_override = None
    
    if len(sys.argv) >= 5:
        name_override = sys.argv[4]

    if len(sys.argv) >= 6:
        version_override = sys.argv[5]

    if not os.path.isfile(config_path):
        help_and_exit("Error: Invalid config file path")

    if not os.path.isdir(template_dir):
        help_and_exit("Error: Invalid template directory path")

    if not os.path.isdir(output_dir):
        help_and_exit("Error: Invalid output directory path")

    return (config_path, template_dir, output_dir, name_override, version_override)

def execute():
    config_path, template_dir, output_dir, name_override, version_override = parse_and_validate_args()

    config_data = load_json(config_path)

    generate_and_write_all(config_data, template_dir, output_dir, package_name = name_override, package_version=version_override)

if __name__ == "__main__":
    execute()
