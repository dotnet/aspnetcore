// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal abstract class SpanChunkGenerator : ISpanChunkGenerator
{
    private static readonly int TypeHashCode = typeof(SpanChunkGenerator).GetHashCode();

    public static readonly ISpanChunkGenerator Null = new NullSpanChunkGenerator();

    public override bool Equals(object obj)
    {
        return obj != null &&
            GetType() == obj.GetType();
    }

    public override int GetHashCode()
    {
        return TypeHashCode;
    }

    private class NullSpanChunkGenerator : ISpanChunkGenerator
    {
        public override string ToString()
        {
            return "None";
        }
    }
}
