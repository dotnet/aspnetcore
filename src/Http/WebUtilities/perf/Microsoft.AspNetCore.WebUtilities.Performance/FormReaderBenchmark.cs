// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FormReaderBenchmark
    {
        [Benchmark]
        public async Task ReadSmallFormAsyncStream()
        {
            var bytes = Encoding.UTF8.GetBytes("foo=bar&baz=boo");
            var stream = new MemoryStream(bytes);

            for (var i = 0; i < 1000; i++)
            {
                var formReader = new FormReader(stream);
                await formReader.ReadFormAsync();
                stream.Position = 0;
            }
        }

        [Benchmark]
        public async Task ReadSmallFormAsyncPipe()
        {
            var pipe = new Pipe();
            var bytes = Encoding.UTF8.GetBytes("foo=bar&baz=boo");

            for (var i = 0; i < 1000; i++)
            {
                pipe.Writer.Write(bytes);
                pipe.Writer.Complete();
                var formReader = new FormPipeReader(pipe.Reader);
                await formReader.ReadFormAsync();
                pipe.Reader.Complete();
                pipe.Reset();
            }
        }
    }
}
