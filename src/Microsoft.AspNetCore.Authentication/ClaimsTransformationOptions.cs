// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Contains the options used by the <see cref="ClaimsTransformationMiddleware"/>.
    /// </summary>
    public class ClaimsTransformationOptions
    {
        /// <summary>
        /// Responsible for transforming the claims principal.
        /// </summary>
        public IClaimsTransformer Transformer { get; set; }
    }
}
