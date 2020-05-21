// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http.Extensions
{
    public class SendFileBenchmark
    {
        [Params(512,
            1024,
            1024 * 2,
            1024 * 4,
            1024 * 8,
            1024 * 16,
            1024 * 32,
            1024 * 64,
            1024 * 128,
            1024 * 256,
            1024 * 512,
            1024 * 1024,
            1024 * 1024 * 2,
            1024 * 1024 * 4,
            1024 * 1024 * 8,
            1024 * 1024 * 16,
            1024 * 1024 * 32,
            1024 * 1024 * 64)]
        public int FileSize { get; set; }

        private string filePath;

        [GlobalSetup]
        public async Task Setup()
        {
            Random random = new Random();
            var content = new byte[FileSize];
            random.NextBytes(content);

            filePath = $"{Path.GetTempPath()}Benchmark TempFile - {FileSize}B.txt";

            await File.WriteAllBytesAsync(filePath, content);
        }

        [Benchmark]
        public async Task Pipe()
        {
            var memorySteam = new MemoryStream();
            var pipeWriter = PipeWriter.Create(memorySteam);
            await SendFileFallback.SendFileAsync(pipeWriter, filePath, 0, null, CancellationToken.None);
        }

        [Benchmark]
        public async Task Stream()
        {
            var memorySteam = new MemoryStream();
            await SendFileFallback.SendFileAsync(memorySteam, filePath, 0, null, CancellationToken.None);
        }
    }
}
