// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal abstract class IntermediateNodeBuilder
{
    public static IntermediateNodeBuilder Create(IntermediateNode root)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        var builder = new DefaultRazorIntermediateNodeBuilder();
        builder.Push(root);
        return builder;
    }

    public abstract IntermediateNode Current { get; }

    public abstract void Add(IntermediateNode node);

    public abstract void Insert(int index, IntermediateNode node);

    public abstract IntermediateNode Build();

    public abstract void Push(IntermediateNode node);

    public abstract IntermediateNode Pop();
}
