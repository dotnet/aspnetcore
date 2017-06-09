// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal class DefaultRazorIRBuilder : RazorIRBuilder
    {
        private readonly List<RazorIRNode> _stack;
        private int _depth;

        public DefaultRazorIRBuilder()
        {
            _stack = new List<RazorIRNode>();
        }

        public override RazorIRNode Current
        {
            get
            {
                return _depth > 0 ? _stack[_depth - 1] : null;
            }
        }

        public override void Add(RazorIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            
            Current.Children.Add(node);
        }

        public override void Insert(int index, RazorIRNode node)
        {
            if (index < 0 || index - Current.Children.Count > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            
            if (index == Current.Children.Count)
            {
                // Allow inserting at 'Children.Count' to be friendlier than List<> typically is.
                Current.Children.Add(node);
            }
            else
            {
                Current.Children.Insert(index, node);
            }
        }

        public override RazorIRNode Build()
        {
            RazorIRNode node = null;
            while (_depth > 0)
            {
                node = Pop();
            }

            return node;
        }

        public override RazorIRNode Pop()
        {
            if (_depth == 0)
            {
                throw new InvalidOperationException(Resources.FormatIRBuilder_PopInvalid(nameof(Pop)));
            }

            var node = _stack[--_depth];
            return node;
        }

        public override void Push(RazorIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (_depth >= _stack.Count)
            {
                _stack.Add(node);
            }
            else
            {
                _stack[_depth] = node;
            }

            if (_depth > 0)
            {
                var parent = _stack[_depth - 1];
                parent.Children.Add(node);
            }

            _depth++;
        }
    }
}
