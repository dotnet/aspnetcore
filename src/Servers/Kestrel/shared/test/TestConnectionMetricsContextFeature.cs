// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.InternalTesting;

internal sealed class TestConnectionMetricsContextFeature : IConnectionMetricsContextFeature
{
    public ConnectionMetricsContext MetricsContext { get; init; }
}
