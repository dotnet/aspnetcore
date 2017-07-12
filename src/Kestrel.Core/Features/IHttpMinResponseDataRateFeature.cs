// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features
{
    /// <summary>
    /// Feature to set the minimum data rate at which the response must be received by the client.
    /// </summary>
    public interface IHttpMinResponseDataRateFeature
    {
        /// <summary>
        /// The minimum data rate in bytes/second at which the response must be received by the client.
        /// Setting this property to null indicates no minimum data rate should be enforced.
        /// This limit has no effect on upgraded connections which are always unlimited.
        /// </summary>
        MinDataRate MinDataRate { get; set; }
    }
}
