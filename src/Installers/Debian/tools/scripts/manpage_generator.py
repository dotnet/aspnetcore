#!/usr/bin/python3
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# manpage_generator
#       Converts top level docs.json format command info to
#       nroff manpage format. Done in python for easy json parsing.
#
#   Usage: argv[1] = path to docs.json; argv[2] = output path

import sys
import os
import json
import datetime

SECTION_SEPARATOR = "\n.P \n"
MANPAGE_EXTENSION = ".1"

# For now this is a magic number
# See https://www.debian.org/doc/manuals/maint-guide/dother.en.html#manpage
SECTION_NUMBER = 1

def generate_man_pages(doc_path, output_dir):

    with open(doc_path) as doc_file:
        doc_json = None
        try:
            doc_json = json.load(doc_file)
        except:
            raise Exception("Failed to load json file. Check formatting.")

        tools = doc_json.get("tools", None)

        if tools is None:
            raise Exception("No tool sections in doc.json")

        for tool_name in tools:
            tool_data = tools[tool_name]

            man_page_content = generate_man_page(tool_name, tool_data)
            man_page_path = get_output_path(tool_name, output_dir)

            write_man_page(man_page_path, man_page_content)

def get_output_path(toolname, output_dir):
    out_filename = toolname + MANPAGE_EXTENSION

    return os.path.join(output_dir, out_filename)

def write_man_page(path, content):
    with open(path, 'w') as man_file:
        man_file.write(content)

	#Build Fails without a final newline
	man_file.write('\n')

def generate_man_page(tool_name, tool_data):

    sections = [
            generate_header_section(tool_name, tool_data),
            generate_name_section(tool_name, tool_data),
            generate_synopsis_section(tool_name, tool_data),
            generate_description_section(tool_name, tool_data),
            generate_options_section(tool_name, tool_data),
            generate_author_section(tool_name, tool_data),
            generate_copyright_section(tool_name, tool_data)
    ]

    return SECTION_SEPARATOR.join(sections)

def generate_header_section(tool_name, tool_data):#
    roff_text_builder = []

    header_format = ".TH {program_name} {section_number} {center_footer} {left_footer} {center_header}"

    today = datetime.date.today()
    today_string = today.strftime("%B %d, %Y")

    format_args = {
            "program_name" : tool_name,
            "section_number" : SECTION_NUMBER,
            "center_footer" : "",   # Omitted
            "left_footer" : "",     # Omitted
            "center_header" : ""    # Omitted
    }

    roff_text_builder.append(header_format.format(**format_args))

    return SECTION_SEPARATOR.join(roff_text_builder)

def generate_name_section(tool_name, tool_data):#
    roff_text_builder = []
    roff_text_builder.append(".SH NAME")

    tool_short_description = tool_data.get("short_description", "")
    name_format = ".B {program_name} - {short_description}"

    name_format_args = {
            "program_name": tool_name,
            "short_description" : tool_short_description
    }

    roff_text_builder.append(name_format.format(**name_format_args))

    return SECTION_SEPARATOR.join(roff_text_builder)

def generate_synopsis_section(tool_name, tool_data):#
    roff_text_builder = []
    roff_text_builder.append(".SH SYNOPSIS")

    synopsis_format = '.B {program_name} {command_name} \n.RI {options} " "\n.I "{argument_list_name}"'

    tool_commands = tool_data.get("commands", [])
    for command_name in tool_commands:
        command_data = tool_commands[command_name]

        # Default options to empty list so the loop doesn't blow up
        options = command_data.get("options", [])
        argument_list = command_data.get("argumentlist", None)

        # Construct Option Strings
        option_string_list = []
        argument_list_name = ""

        for option_name in options:
            option_data = options[option_name]

            specifier_short = option_data.get("short", None)
            specifier_long = option_data.get("long", None)
            parameter = option_data.get("parameter", None)

            option_string = _option_string_helper(specifier_short, specifier_long, parameter)

            option_string_list.append(option_string)

        # Populate Argument List Name
        if argument_list:
            argument_list_name = argument_list.get("name", "")

        cmd_format_args = {
                'program_name' : tool_name,
                'command_name' : command_name,
                'options' : '" "'.join(option_string_list),
                'argument_list_name' : argument_list_name
        }

        cmd_string = synopsis_format.format(**cmd_format_args)

        roff_text_builder.append(cmd_string)

    return SECTION_SEPARATOR.join(roff_text_builder)

def generate_description_section(tool_name, tool_data):#
    roff_text_builder = []
    roff_text_builder.append(".SH DESCRIPTION")

    # Tool Description
    long_description = tool_data.get("long_description", "")
    roff_text_builder.append(".PP {0}".format(long_description))

    # Command Descriptions
    cmd_description_format = ".B {program_name} {command_name}\n{command_description}"

    tool_commands = tool_data.get("commands", [])
    for command_name in tool_commands:
        command_data = tool_commands[command_name]

        command_description = command_data.get("description", "")

        format_args = {
            "program_name" : tool_name,
            "command_name" : command_name,
            "command_description" : command_description
        }

        cmd_string = cmd_description_format.format(**format_args)

        roff_text_builder.append(cmd_string)

    return SECTION_SEPARATOR.join(roff_text_builder)

def generate_options_section(tool_name, tool_data):#
    roff_text_builder = []
    roff_text_builder.append(".SH OPTIONS")

    options_format = '.TP\n.B {option_specifiers}\n{option_description}'

    tool_commands = tool_data.get("commands", [])
    for command_name in tool_commands:
        command_data = tool_commands[command_name]

        # Default to empty list so the loop doesn't blow up
        options = command_data.get("options", [])

        for option_name in options:
            option_data = options[option_name]

            specifier_short = option_data.get("short", None)
            specifier_long = option_data.get("long", None)
            parameter = option_data.get("parameter", None)
            description = option_data.get("description", "")

            option_specifiers_string = _option_string_helper(specifier_short, 
                specifier_long, 
                parameter, 
                include_brackets = False, 
                delimiter=' ", " ')

            format_args = {
                "option_specifiers": option_specifiers_string,
                "option_description" : description
            }

            roff_text_builder.append(options_format.format(**format_args))

    return SECTION_SEPARATOR.join(roff_text_builder)

def generate_author_section(tool_name, tool_data):#
    roff_text_builder = []
    roff_text_builder.append(".SH AUTHOR")
    
    author_format = '.B "{author_name}" " " \n.RI ( "{author_email}" )'
    
    author_name = tool_data.get("author", "")
    author_email = tool_data.get("author_email", "")
    
    format_args = {
        "author_name" : author_name,
        "author_email" : author_email
    }
    
    roff_text_builder.append(author_format.format(**format_args))

    return SECTION_SEPARATOR.join(roff_text_builder)

def generate_copyright_section(tool_name, tool_data):#
    roff_text_builder = []
    roff_text_builder.append(".SH COPYRIGHT")
    
    copyright_data = tool_data.get("copyright")
    
    roff_text_builder.append('.B "{0}"'.format(copyright_data))

    return SECTION_SEPARATOR.join(roff_text_builder)

def _option_string_helper(specifier_short, specifier_long, parameter, include_brackets = True, delimiter = " | "):
    option_string = ""

    if include_brackets:
        option_string = " [ "

    if specifier_short:
        option_string += ' "{0}" '.format(specifier_short)

    if specifier_short and specifier_long:
        option_string += delimiter

    if specifier_long:
        option_string += ' "{0}" '.format(specifier_long)

    if parameter:
    	option_string += ' " " '
        option_string += ' "{0}" '.format(parameter)

    if include_brackets:
        option_string += " ] "

    return option_string


def print_usage():
    print("Usage: argv[1] = path to docs.json; argv[2] = output path")
    print("Example: manpage_generator.py ../docs.json ./dotnet-1.0/debian")

def parse_args():
    doc_path = sys.argv[1]
    output_dir = sys.argv[2]

    return (doc_path, output_dir)

def validate_args(doc_path, output_dir):
    if not os.path.isfile(doc_path):
        raise Exception("Docs.json path is not valid.")

    if not os.path.isdir(output_dir):
        raise Exception("Output Directory Path is not valid.")

def execute_command_line():
    try:
        doc_path, output_dir = parse_args()

        validate_args(doc_path, output_dir)

        generate_man_pages(doc_path, output_dir)

    except Exception as exc:
        print("Error: ", exc)
        print_usage()

if __name__ == "__main__":
    execute_command_line()
