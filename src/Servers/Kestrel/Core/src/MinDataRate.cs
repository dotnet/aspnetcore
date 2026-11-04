// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// The minimum data rate for incoming connections.
/// </summary>
public class MinDataRate
{
    /// <summary>
    /// Creates a new instance of <see cref="MinDataRate"/>.
    /// </summary>
    /// <param name="bytesPerSecond">The minimum rate in bytes/second at which data should be processed.</param>
    /// <param name="gracePeriod">The amount of time to delay enforcement of <paramref name="bytesPerSecond"/>,
    /// starting at the time data is first read or written.</param>
    public MinDataRate(double bytesPerSecond, TimeSpan gracePeriod)
    {
        if (bytesPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesPerSecond), CoreStrings.PositiveNumberOrNullMinDataRateRequired);
        }

        if (gracePeriod <= Heartbeat.Interval)
        {
            throw new ArgumentOutOfRangeException(nameof(gracePeriod), CoreStrings.FormatMinimumGracePeriodRequired(Heartbeat.Interval.TotalSeconds));
        }

        BytesPerSecond = bytesPerSecond;
        GracePeriod = gracePeriod;
    }

    /// <summary>
    /// The minimum rate in bytes/second at which data should be processed.
    /// </summary>
    public double BytesPerSecond { get; }

    /// <summary>
    /// The amount of time to delay enforcement of <see cref="MinDataRate" />,
    /// starting at the time data is first read or written.
    /// </summary>
    public TimeSpan GracePeriod { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Bytes per second: {BytesPerSecond}, Grace Period: {GracePeriod}";
    }
}
