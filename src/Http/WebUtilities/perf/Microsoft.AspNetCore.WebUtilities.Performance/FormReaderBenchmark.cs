// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FormReaderBenchmark
    {
        private byte[] _smallBytes;
        private byte[] _largeBytes;

        [GlobalSetup]
        public void Setup()
        {
            _smallBytes = Encoding.UTF8.GetBytes("foo=bar&baz=boo");
            _largeBytes = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("%22%25%2D%2E%3C%3E%5C%5E%5F%60%7B%7C%7D%7E=%22%25%2D%2E%3C%3E%5C%5E%5F%60%7B%7C%7D%7E&", 200)) + "foo=bar");
        }

        [Benchmark]
        public void CreateSmallForm()
        {
            var stream = new MemoryStream(_smallBytes);
            var formReader = new FormReader(stream);
        }

        [Benchmark]
        public async Task ReadSmallFormAsync()
        {
            var stream = new MemoryStream(_smallBytes);
            var formReader = new FormReader(stream);
            await formReader.ReadFormAsync();
        }

        [Benchmark]
        public void CreateLargeForm()
        {
            var stream = new MemoryStream(_largeBytes);
            var formReader = new FormReader(stream);
            formReader.ValueCountLimit = 5000;
        }

        [Benchmark]
        public async Task ReadLargeFormAsync()
        {
            var stream = new MemoryStream(_largeBytes);
            var formReader = new FormReader(stream);
            formReader.ValueCountLimit = 5000;

            await formReader.ReadFormAsync();
        }
    }
}
