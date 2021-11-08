// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class LiteralAttributeChunkGenerator : SpanChunkGenerator
{
    public LiteralAttributeChunkGenerator(LocationTagged<string> prefix, LocationTagged<string> value)
    {
        Prefix = prefix;
        Value = value;
    }

    public LocationTagged<string> Prefix { get; }

    public LocationTagged<string> Value { get; }

    public override string ToString()
    {
        return string.Format(CultureInfo.CurrentCulture, "LitAttr:{0:F}", Prefix);
    }

    public override bool Equals(object obj)
    {
        var other = obj as LiteralAttributeChunkGenerator;
        return other != null &&
            Equals(other.Prefix, Prefix) &&
            Equals(other.Value, Value);
    }

    public override int GetHashCode()
    {
        var hashCodeCombiner = HashCodeCombiner.Start();

        hashCodeCombiner.Add(Prefix);
        hashCodeCombiner.Add(Value);

        return hashCodeCombiner;
    }
}
