// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Feature to set the minimum data rate at which the the request body must be sent by the client.
/// This feature is not supported for HTTP/2 requests except to disable it entirely by setting <see cref="MinDataRate"/> to <see langword="null"/>
/// Instead, use <see cref="KestrelServerLimits.MinRequestBodyDataRate"/> for server-wide configuration which applies to both HTTP/2 and HTTP/1.x.
/// </summary>
public interface IHttpMinRequestBodyDataRateFeature
{
    /// <summary>
    /// The minimum data rate in bytes/second at which the request body must be sent by the client.
    /// Setting this property to null indicates no minimum data rate should be enforced.
    /// This limit has no effect on upgraded connections which are always unlimited.
    /// This feature is not supported for HTTP/2 requests except to disable it entirely by setting <see cref="MinDataRate"/> to <see langword="null"/>
    /// Instead, use <see cref="KestrelServerLimits.MinRequestBodyDataRate"/> for server-wide configuration which applies to both HTTP/2 and HTTP/1.x.
    /// </summary>
    MinDataRate? MinDataRate { get; set; }
}
