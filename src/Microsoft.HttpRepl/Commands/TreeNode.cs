// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.HttpRepl.Commands
{
    public class TreeNode
    {
        private readonly int _depth;
        private readonly Formatter _formatter;
        private readonly string _prefix;
        private readonly string _entry;
        private readonly List<TreeNode> _children = new List<TreeNode>();

        public TreeNode(Formatter formatter, string prefix, string entry)
            : this(formatter, prefix, entry, 0)
        {
        }

        private TreeNode(Formatter formatter, string prefix, string entry, int depth)
        {
            _formatter = formatter;
            formatter.RegisterEntry(prefix.Length, depth);
            _prefix = prefix;
            _entry = entry;
            _depth = depth;
        }

        public TreeNode AddChild(string prefix, string entry)
        {
            TreeNode child = new TreeNode(_formatter, prefix, entry, _depth + 1);
            _children.Add(child);
            return child;
        }

        public override string ToString()
        {
            string self = _formatter.Format(_prefix, _entry, _depth);

            if (_children.Count == 0)
            {
                return self;
            }

            return self + Environment.NewLine + string.Join(Environment.NewLine, _children);
        }
    }
}
