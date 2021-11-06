// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public struct IntermediateNodeReference
{
    public IntermediateNodeReference(IntermediateNode parent, IntermediateNode node)
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

    public void Deconstruct(out IntermediateNode parent, out IntermediateNode node)
    {
        parent = Parent;
        node = Node;
    }

    public IntermediateNode Node { get; }

    public IntermediateNode Parent { get; }

    public IntermediateNodeReference InsertAfter(IntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (Parent == null)
        {
            throw new InvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index == -1)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(
                Node,
                Parent));
        }

        Parent.Children.Insert(index + 1, node);
        return new IntermediateNodeReference(Parent, node);
    }

    public void InsertAfter(IEnumerable<IntermediateNode> nodes)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException(nameof(nodes));
        }

        if (Parent == null)
        {
            throw new InvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index == -1)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(
                Node,
                Parent));
        }

        foreach (var node in nodes)
        {
            Parent.Children.Insert(++index, node);
        }
    }

    public IntermediateNodeReference InsertBefore(IntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (Parent == null)
        {
            throw new InvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index == -1)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(
                Node,
                Parent));
        }

        Parent.Children.Insert(index, node);
        return new IntermediateNodeReference(Parent, node);
    }

    public void InsertBefore(IEnumerable<IntermediateNode> nodes)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException(nameof(nodes));
        }

        if (Parent == null)
        {
            throw new InvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index == -1)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(
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
            throw new InvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index == -1)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(
                Node,
                Parent));
        }

        Parent.Children.RemoveAt(index);
    }

    public IntermediateNodeReference Replace(IntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (Parent == null)
        {
            throw new InvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index == -1)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(
                Node,
                Parent));
        }

        Parent.Children[index] = node;
        return new IntermediateNodeReference(Parent, node);
    }
}
