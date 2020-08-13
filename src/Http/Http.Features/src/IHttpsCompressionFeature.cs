// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Configures response compression behavior for HTTPS on a per-request basis.
    /// </summary>
    public interface IHttpsCompressionFeature
    {
        /// <summary>
        /// The <see cref="HttpsCompressionMode"/> to use.
        /// </summary>
        HttpsCompressionMode Mode { get; set; }
    }
}
