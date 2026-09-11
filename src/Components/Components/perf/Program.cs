// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.BenchmarkDotNet.Runner;

internal partial class Program
{
    static partial void BeforeMain(string[] args)
    {
        if (args.Length == 0 || args[0] != "--profile")
        {
            return;
        }

        // Write code here if you want to profile something. Normally Benchmark.NET launches
        // a separate process, which can be hard to profile.
        //
        // See: https://github.com/dotnet/BenchmarkDotNet/issues/387

        // Example:
        //Console.WriteLine("Starting...");
        //var stopwatch = Stopwatch.StartNew();
        //var benchmark = new RenderTreeDiffBuilderBenchmark();

        //for (var i = 0; i < 100000; i++)
        //{
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //    benchmark.ComputeDiff_SingleFormField();
        //}

        //Console.WriteLine($"Done after {stopwatch.ElapsedMilliseconds}ms");
        //Environment.Exit(0);

        // Call each variant many times across all param sizes to trigger Tier1 JIT compilation.
        // Run with: $env:DOTNET_JitDisasm="WriteFrame"; dotnet run -c Release -- --profile
        // foreach (var count in new[] { 64, 512, 4096 })
        // {
        //     var b = new RenderBatchWriterBenchmark { ReferenceFrameCount = count };
        //     b.Setup();
        //     for (var i = 0; i < 10000; i++)
        //     {
        //         b.WriteFrames_In();
        //         b.WriteFrames_DirectRef();
        //         b.WriteFrames_LocalCopyRef();
        //         b.WriteFrames_ByValue();
        //     }
        // }
        // Console.WriteLine("JIT disasm warmup complete.");
        // Environment.Exit(0);
    }
}
