// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNet.Antiforgery
{
    /// <summary>
    /// This interface can extract unique identifers for a claims-based identity.
    /// </summary>
    public interface IClaimUidExtractor
    {
        /// <summary>
        /// Extracts claims identifier.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/>.</param>
        /// <returns>The claims identifier.</returns>
        string ExtractClaimUid(ClaimsIdentity identity);
    }
}