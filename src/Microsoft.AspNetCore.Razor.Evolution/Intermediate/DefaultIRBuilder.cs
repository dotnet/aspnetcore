// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    internal class DefaultIRBuilder : IRBuilder
    {
        private readonly List<IRNode> _stack;
        private int _depth;

        public DefaultIRBuilder()
        {
            _stack = new List<IRNode>();
        }

        public override IRNode Current
        {
            get
            {
                return _depth > 0 ? _stack[_depth - 1] : null;
            }
        }

        public override void Add(IRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Push(node);
            Pop();
        }

        public override IRNode Pop()
        {
            if (_depth == 0)
            {
                throw new InvalidOperationException(Resources.FormatIRBuilder_PopInvalid(nameof(Pop)));
            }

            var node = _stack[--_depth];
            return node;
        }

        public override void Push(IRNode node)
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
                node.Parent = parent;
                parent.Children.Add(node);
            }

            _depth++;
        }
    }
}
