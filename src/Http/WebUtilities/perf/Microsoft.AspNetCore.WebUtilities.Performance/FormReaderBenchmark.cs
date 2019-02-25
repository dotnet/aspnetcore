// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FormReaderBenchmark
    {
        private FormReader _formReader;

        [IterationSetup]
        public void Setup()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("foo=bar&baz=boo"));
            _formReader = new FormReader(stream);
        }

        [Benchmark]
        public async Task<Dictionary<string, StringValues>> ReadSmallFormAsync()
        {
            var dictionary = await _formReader.ReadFormAsync();
            return dictionary;
        }
    }
}
