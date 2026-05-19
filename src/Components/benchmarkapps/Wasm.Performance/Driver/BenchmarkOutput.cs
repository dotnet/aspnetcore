// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Performance.Driver;

internal sealed class BenchmarkOutput
{
    public List<BenchmarkMetadata> Metadata { get; } = new List<BenchmarkMetadata>();

    public List<BenchmarkMeasurement> Measurements { get; } = new List<BenchmarkMeasurement>();
}
