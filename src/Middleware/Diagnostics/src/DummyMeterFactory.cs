// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Diagnostics;

internal sealed class DummyMeterFactory : IMeterFactory
{
    public Meter Create(MeterOptions options) => new Meter(options);

    public void Dispose() { }
}
