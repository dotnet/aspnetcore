// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public struct RazorIRNodeReference
    {
        public RazorIRNodeReference(RazorIRNode parent, RazorIRNode node)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Parent = parent;
            Node = node;
        }

        public void Deconstruct(out RazorIRNode parent, out RazorIRNode node)
        {
            parent = Parent;
            node = Node;
        }

        public RazorIRNode Node { get; }

        public RazorIRNode Parent { get; }

        public RazorIRNodeReference InsertAfter(RazorIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (Parent == null)
            {
                throw new InvalidOperationException(Resources.IRNodeReference_NotInitialized);
            }

            if (Parent.Children.IsReadOnly)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_CollectionIsReadOnly(Parent));
            }

            var index = Parent.Children.IndexOf(Node);
            if (index == -1)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_NodeNotFound(
                    Node,
                    Parent));
            }

            Parent.Children.Insert(index + 1, node);
            return new RazorIRNodeReference(Parent, node);
        }

        public void InsertAfter(IEnumerable<RazorIRNode> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (Parent == null)
            {
                throw new InvalidOperationException(Resources.IRNodeReference_NotInitialized);
            }

            if (Parent.Children.IsReadOnly)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_CollectionIsReadOnly(Parent));
            }

            var index = Parent.Children.IndexOf(Node);
            if (index == -1)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_NodeNotFound(
                    Node,
                    Parent));
            }

            foreach (var node in nodes)
            {
                Parent.Children.Insert(++index, node);
            }
        }

        public RazorIRNodeReference InsertBefore(RazorIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (Parent == null)
            {
                throw new InvalidOperationException(Resources.IRNodeReference_NotInitialized);
            }

            if (Parent.Children.IsReadOnly)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_CollectionIsReadOnly(Parent));
            }

            var index = Parent.Children.IndexOf(Node);
            if (index == -1)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_NodeNotFound(
                    Node,
                    Parent));
            }

            Parent.Children.Insert(index, node);
            return new RazorIRNodeReference(Parent, node);
        }

        public void InsertBefore(IEnumerable<RazorIRNode> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (Parent == null)
            {
                throw new InvalidOperationException(Resources.IRNodeReference_NotInitialized);
            }

            if (Parent.Children.IsReadOnly)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_CollectionIsReadOnly(Parent));
            }

            var index = Parent.Children.IndexOf(Node);
            if (index == -1)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_NodeNotFound(
                    Node,
                    Parent));
            }

            foreach (var node in nodes)
            {
                Parent.Children.Insert(index++, node);
            }
        }

        public void Remove()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException(Resources.IRNodeReference_NotInitialized);
            }

            if (Parent.Children.IsReadOnly)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_CollectionIsReadOnly(Parent));
            }

            var index = Parent.Children.IndexOf(Node);
            if (index == -1)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_NodeNotFound(
                    Node,
                    Parent));
            }

            Parent.Children.RemoveAt(index);
        }

        public RazorIRNodeReference Replace(RazorIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (Parent == null)
            {
                throw new InvalidOperationException(Resources.IRNodeReference_NotInitialized);
            }

            if (Parent.Children.IsReadOnly)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_CollectionIsReadOnly(Parent));
            }

            var index = Parent.Children.IndexOf(Node);
            if (index == -1)
            {
                throw new InvalidOperationException(Resources.FormatIRNodeReference_NodeNotFound(
                    Node,
                    Parent));
            }

            Parent.Children[index] = node;
            return new RazorIRNodeReference(Parent, node);
        }
    }
}
