// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
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
}
