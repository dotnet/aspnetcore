// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Performance.Driver;

class BenchmarkScenarioResult
{
    public string Name { get; set; }

    public BenchmarkDescriptor Descriptor { get; set; }

    public string ShortDescription { get; set; }

    public bool Success { get; set; }

    public int NumExecutions { get; set; }

    public double Duration { get; set; }

    public class BenchmarkDescriptor
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
