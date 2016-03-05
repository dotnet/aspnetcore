// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Used for claims transformation.
    /// </summary>
    public interface IClaimsTransformer
    {
        /// <summary>
        /// Provides a central transformation point to change the specified principal.
        /// </summary>
        /// <param name="context"><see cref="ClaimsTransformationContext"/> containing principal to transform and current HttpContext.</param>
        /// <returns>The transformed principal.</returns>
        Task<ClaimsPrincipal> TransformAsync(ClaimsTransformationContext context);
    }
}
