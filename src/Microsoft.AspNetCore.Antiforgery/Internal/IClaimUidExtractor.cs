// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    /// <summary>
    /// This interface can extract unique identifers for a <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public interface IClaimUidExtractor
    {
        /// <summary>
        /// Extracts claims identifier.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <returns>The claims identifier.</returns>
        string ExtractClaimUid(ClaimsPrincipal claimsPrincipal);
    }
}