#!/usr/bin/python3
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Extract Json Value
#
# Very simple tool to ease extracting json values from the cmd line.
import os
import sys
import json

def print_usage():
	print("""
		Usage: extract_json_value.py [json file path] [key of value to extract]
		For nested keys, use . separator
	""")

def help_and_exit(msg=None):
	print(msg)
	print_usage()
	sys.exit(1)

def parse_and_validate_args():
	
	if len(sys.argv) < 3:
		help_and_exit(msg="Error: Invalid Args")

	json_path = sys.argv[1]
	json_key = sys.argv[2]

	if not os.path.isfile(json_path):
		help_and_exit("Error: Invalid json file path")

	return json_path, json_key

def extract_key(json_path, json_key):
	json_data = None

	with open(json_path, 'r') as json_file:
		json_data = json.load(json_file)

	nested_keys = json_key.split('.')
	json_context = json_data

	for key in nested_keys:
		json_context = json_context.get(key, None)

		if json_context is None:
			help_and_exit("Error: Invalid json key")

	return str(json_context)

def execute():
	json_path, json_key = parse_and_validate_args()

	value = extract_key(json_path, json_key)

	return value

if __name__ == "__main__":
	print(execute())

