using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http.Abstractions.Microbenchmarks
{
    public class PathStringBenchmark
    {
        private const string TestPath = "/api/a%2Fb/c";
        private const string LongTestPath = "/thisMustBeAVeryLongPath/SoLongThatItCouldActuallyBeLargerToTheStackAllocThresholdValue/PathsShorterToThisAllocateLessOnHeapByUsingStackAllocation/api/a%20b";

        public IEnumerable<object> TestPaths => new[] { TestPath, LongTestPath };

        public IEnumerable<object> TestUris => new[] { new Uri($"https://localhost:5001/{TestPath}"), new Uri($"https://localhost:5001/{LongTestPath}") };

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
}
