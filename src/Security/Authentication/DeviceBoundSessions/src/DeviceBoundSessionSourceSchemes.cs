// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

internal sealed class DeviceBoundSessionSourceSchemes
{
    /// <summary>Maps source cookie scheme → DBSC handler scheme.</summary>
    public IDictionary<string, string> Schemes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Maps refresh cookie scheme → source cookie scheme.</summary>
    public IDictionary<string, string> RefreshSchemes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Maps session cookie scheme → source cookie scheme.</summary>
    public IDictionary<string, string> SessionSchemes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
