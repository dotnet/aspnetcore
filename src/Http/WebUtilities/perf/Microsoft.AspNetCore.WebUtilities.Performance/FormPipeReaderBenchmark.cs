// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FormPipeReaderBenchmark
    {
        private FormPipeReader _formPipeReader;

        [IterationSetup]
        public async Task Setup()
        {
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("foo=bar&baz=boo"));
            pipe.Writer.Complete();
            _formPipeReader = new FormPipeReader(pipe.Reader);
        }

        [Benchmark]
        public async Task<Dictionary<string, StringValues>> ReadSmallFormAsync()
        {
            var dictionary = await _formPipeReader.ReadFormAsync();
            return dictionary;
        }
    }
}
