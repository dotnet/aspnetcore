// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Used to capture path info so redirects can be computed properly within an app.Map().
    /// </summary>
    public interface IAuthenticationFeature
    {
        /// <summary>
        /// The original path base.
        /// </summary>
        PathString OriginalPathBase { get; set; }

        /// <summary>
        /// The original path.
        /// </summary>
        PathString OriginalPath { get; set; }
    }
}
