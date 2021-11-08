// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class DirectiveTokenChunkGenerator : SpanChunkGenerator
{
    private static readonly Type Type = typeof(DirectiveTokenChunkGenerator);

    public DirectiveTokenChunkGenerator(DirectiveTokenDescriptor tokenDescriptor)
    {
        Descriptor = tokenDescriptor;
    }

    public DirectiveTokenDescriptor Descriptor { get; }

    public override bool Equals(object obj)
    {
        var other = obj as DirectiveTokenChunkGenerator;
        return base.Equals(other) &&
            DirectiveTokenDescriptorComparer.Default.Equals(Descriptor, other.Descriptor);
    }

    public override int GetHashCode()
    {
        var combiner = HashCodeCombiner.Start();
        combiner.Add(base.GetHashCode());
        combiner.Add(Type);

        return combiner.CombinedHash;
    }

    public override string ToString()
    {
        var builder = new StringBuilder("DirectiveToken {");
        builder.Append(Descriptor.Name);
        builder.Append(';');
        builder.Append(Descriptor.Kind);
        builder.Append(";Opt:");
        builder.Append(Descriptor.Optional);
        builder.Append('}');

        return builder.ToString();
    }
}
