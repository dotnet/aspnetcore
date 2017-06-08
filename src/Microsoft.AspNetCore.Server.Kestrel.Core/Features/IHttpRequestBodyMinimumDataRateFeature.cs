// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features
{
    /// <summary>
    /// Represents a minimum data rate for the request body of an HTTP request.
    /// </summary>
    public interface IHttpRequestBodyMinimumDataRateFeature
    {
        /// <summary>
        /// The minimum data rate in bytes/second at which the request body should be received.
        /// Setting this property to null indicates no minimum data rate should be enforced.
        /// This limit has no effect on upgraded connections which are always unlimited.
        /// </summary>
        MinimumDataRate MinimumDataRate { get; set; }
    }
}
