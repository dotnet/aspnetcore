// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features
{
    /// <summary>
    /// Feature to set the minimum data rate at which the the request body must be sent by the client.
    /// This feature is not available for HTTP/2 requests. Instead, use <see cref="KestrelServerLimits.MinRequestBodyDataRate"/>
    /// for server-wide configuration which applies to both HTTP/2 and HTTP/1.x.
    /// </summary>
    public interface IHttpMinRequestBodyDataRateFeature
    {
        /// <summary>
        /// The minimum data rate in bytes/second at which the request body must be sent by the client.
        /// Setting this property to null indicates no minimum data rate should be enforced.
        /// This limit has no effect on upgraded connections which are always unlimited.
        /// This feature is not available for HTTP/2 requests. Instead, use <see cref="KestrelServerLimits.MinRequestBodyDataRate"/>
        /// for server-wide configuration which applies to both HTTP/2 and HTTP/1.x.
        /// </summary>
        MinDataRate MinDataRate { get; set; }
    }
}
