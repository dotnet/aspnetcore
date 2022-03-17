// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

public class HeaderUtilitiesBenchmark
{
    [Benchmark]
    public StringSegment UnescapeAsQuotedString()
    {
        return HeaderUtilities.UnescapeAsQuotedString("\"hello\\\"foo\\\\bar\\\\baz\\\\\"");
    }

    [Benchmark]
    public StringSegment EscapeAsQuotedString()
    {
        return HeaderUtilities.EscapeAsQuotedString("\"hello\\\"foo\\\\bar\\\\baz\\\\\"");
    }
}
