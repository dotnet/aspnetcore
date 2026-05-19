// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Performance.Driver;

internal sealed class BenchmarkMeasurement
{
    public DateTime Timestamp { get; internal set; }
    public string Name { get; internal set; }
    public object Value { get; internal set; }
}
