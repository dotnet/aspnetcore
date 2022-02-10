// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Feature to set the minimum data rate at which the response must be received by the client.
/// This feature is not available for HTTP/2 requests. Instead, use <see cref="KestrelServerLimits.MinResponseDataRate"/>
/// for server-wide configuration which applies to both HTTP/2 and HTTP/1.x.
/// </summary>
public interface IHttpMinResponseDataRateFeature
{
    /// <summary>
    /// The minimum data rate in bytes/second at which the response must be received by the client.
    /// Setting this property to null indicates no minimum data rate should be enforced.
    /// This limit has no effect on upgraded connections which are always unlimited.
    /// This feature is not available for HTTP/2 requests. Instead, use <see cref="KestrelServerLimits.MinResponseDataRate"/>
    /// for server-wide configuration which applies to both HTTP/2 and HTTP/1.x.
    /// </summary>
    MinDataRate? MinDataRate { get; set; }
}
