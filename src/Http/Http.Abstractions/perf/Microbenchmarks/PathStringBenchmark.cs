// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http.Abstractions.Microbenchmarks;

public class PathStringBenchmark
{
    private const string TestPath = "/api/a%2Fb/c";
    private const string LongTestPath = "/thisMustBeAVeryLongPath/SoLongThatItCouldActuallyBeLargerToTheStackAllocThresholdValue/PathsShorterToThisAllocateLessOnHeapByUsingStackAllocation/api/a%20b";
    private const string LongTestPathEarlyPercent = "/t%20hisMustBeAVeryLongPath/SoLongButStillShorterToTheStackAllocThresholdValue/PathsShorterToThisAllocateLessOnHeap/api/a%20b";

    public IEnumerable<object> TestPaths => new[] { TestPath, LongTestPath, LongTestPathEarlyPercent };

    public IEnumerable<object> TestUris => new[] { new Uri($"https://localhost:5001/{TestPath}"), new Uri($"https://localhost:5001/{LongTestPath}"), new Uri($"https://localhost:5001/{LongTestPathEarlyPercent}") };

    [Benchmark]
    [ArgumentsSource(nameof(TestPaths))]
    public string OnPathFromUriComponent(string testPath)
    {
        var pathString = PathString.FromUriComponent(testPath);
        return pathString.Value;
    }

    [Benchmark]
    [ArgumentsSource(nameof(TestUris))]
    public string OnUriFromUriComponent(Uri testUri)
    {
        var pathString = PathString.FromUriComponent(testUri);
        return pathString.Value;
    }
}
