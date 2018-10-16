#!/usr/bin/env python3

import collections
import glob
import yaml
from os import path
from termcolor import colored, cprint
from yaml.constructor import Constructor

def dump_format(dump, tag, mapping, flow_style=None):
    """
    Better output formatting for YAML dictionaries
    """
    value = []
    node = yaml.MappingNode(tag, value, flow_style=flow_style)
    if dump.alias_key is not None:
        dump.represented_objects[dump.alias_key] = node
    best_style = True
    if hasattr(mapping, 'items'):
        mapping = mapping.items()
    for item_key, item_value in mapping:
        node_key = dump.represent_data(item_key)
        node_value = dump.represent_data(item_value)
        if not (isinstance(node_key, yaml.ScalarNode) and not node_key.style):
            best_style = False
        if not (isinstance(node_value, yaml.ScalarNode) and not node_value.style):
            best_style = False
        value.append((node_key, node_value))
    if flow_style is None:
        if dump.default_flow_style is not None:
            node.flow_style = dump.default_flow_style
        else:
            node.flow_style = best_style
    return node


def add_bool_as_scalar(self, node):
    """
    Don't auto-parse boolean values
    """
    if node.value == 'true' or node.value == 'false' :
        return self.construct_yaml_bool(node)
    return self.construct_scalar(node)

_mapping_tag = yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG

def dict_representer(dumper, data):
    return dumper.represent_mapping(_mapping_tag, data.iteritems())


def dict_constructor(loader, node):
    return collections.OrderedDict(loader.construct_pairs(node))

def update(pattern, updater):
    print('\n\n\n')
    cprint(pattern, 'magenta')

    for f in glob.glob(path.join(repo_root, "modules", "*", pattern)):
        yml = path.join(repo_root, f)

        if not path.exists(yml):
            cprint("File does not exist: {}".format(yml), 'red')
            continue

        print("Updating {}".format(yml))
        document = yaml.load(open(yml, 'r'))
        document = updater(document)
        yml_file = open(yml, 'w')
        yml_file.write(yaml.safe_dump(document, default_flow_style=False, indent=2))
        yml_file.close()

#
# Config yaml parser
#

# Do not reorder keys in yaml file
yaml.add_representer(collections.OrderedDict, dict_representer)
yaml.add_constructor(_mapping_tag, dict_constructor)
# Pretty print dictionaries
yaml.SafeDumper.add_representer(collections.OrderedDict,
                                lambda dumper, value: dump_format(dumper, u'tag:yaml.org,2002:map', value))
# Don't parse booleans - treat them as scalars
yaml.Loader.add_constructor(u'tag:yaml.org,2002:bool', add_bool_as_scalar)
yaml.SafeLoader.add_constructor(u'tag:yaml.org,2002:bool', add_bool_as_scalar)

#
# Main
#

repo_root = path.dirname(path.dirname(path.abspath(__file__)))

def transform_yaml_doc(document):
    if not 'branches' in document:
        document['branches'] = {}
    document['branches']['only'] = [
        'master', '/^release\/.*$/', '/^(.*\/)?ci-.*$/']
    return document

update(".travis.yml", transform_yaml_doc)
update(".appveyor.yml", transform_yaml_doc)
