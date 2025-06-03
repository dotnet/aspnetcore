// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal static class MetricsConstants
{
    // Follows boundaries from http.server.request.duration/http.client.request.duration
    public static readonly IReadOnlyList<double> ShortSecondsBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];

    // Not based on a standard. Larger bucket sizes for longer lasting operations, e.g. HTTP connection duration. See https://github.com/open-telemetry/semantic-conventions/issues/336
    public static readonly IReadOnlyList<double> LongSecondsBucketBoundaries = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 120, 300];

    // For blazor rendering, which should be very fast.
    public static readonly IReadOnlyList<double> BlazorRenderingSecondsBucketBoundaries = [0.000001, 0.00001, 0.0001, 0.001, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];

    // For blazor circuit sessions, which can last a long time.
    public static readonly IReadOnlyList<double> BlazorCircuitSecondsBucketBoundaries = [1, 3, 10, 30, 1 * 60, 3 * 60, 10 * 60, 30 * 60, 1 * 60 * 60, 3 * 60 * 60, 10 * 60 * 60, 24 * 60 * 60];
}
