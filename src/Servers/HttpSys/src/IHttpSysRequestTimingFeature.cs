// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// This exposes the Http.Sys HTTP_REQUEST_TIMING_INFO extensibility point which contains request processing timestamp data from Http.Sys.
/// </summary>
public interface IHttpSysRequestTimingFeature
{
    /// <summary>
    /// Gets all Http.Sys timing timestamps
    /// </summary>
    /// <remarks>
    /// These timestamps were obtained using QueryPerformanceCounter <see href="https://learn.microsoft.com/windows/win32/api/profileapi/nf-profileapi-queryperformancecounter"/> and the timestamp frequency can be obtained via QueryPerformanceFrequency <see href="https://learn.microsoft.com/windows/win32/api/profileapi/nf-profileapi-queryperformancefrequency"/>.
    /// The index of the timing can be cast to <see cref="HttpSysRequestTimingType"/> to know what the timing represents.
    /// The value may be 0 if the timing is not available for the current request.
    /// </remarks>
    ReadOnlySpan<long> Timestamps { get; }

    /// <summary>
    /// Gets the timestamp for the given timing.
    /// </summary>
    /// <remarks>
    /// These timestamps were obtained using QueryPerformanceCounter <see href="https://learn.microsoft.com/windows/win32/api/profileapi/nf-profileapi-queryperformancecounter"/> and the timestamp frequency can be obtained via QueryPerformanceFrequency <see href="https://learn.microsoft.com/windows/win32/api/profileapi/nf-profileapi-queryperformancefrequency"/>.
    /// </remarks>
    /// <param name="timestampType">The timestamp type to get.</param>
    /// <param name="timestamp">The value of the timestamp if set.</param>
    /// <returns>True if the given timing was set (i.e., non-zero).</returns>
    bool TryGetTimestamp(HttpSysRequestTimingType timestampType, out long timestamp);

    /// <summary>
    /// Gets the elapsed time between the two given timings.
    /// </summary>
    /// <param name="startingTimestampType">The timestamp type marking the beginning of the time period.</param>
    /// <param name="endingTimestampType">The timestamp type marking the end of the time period.</param>
    /// <param name="elapsed">A <see cref="TimeSpan"/> for the elapsed time between the starting and ending timestamps.</param>
    /// <returns>True if both given timings were set (i.e., non-zero).</returns>
    bool TryGetElapsedTime(HttpSysRequestTimingType startingTimestampType, HttpSysRequestTimingType endingTimestampType, out TimeSpan elapsed);
}
