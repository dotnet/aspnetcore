// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims
{
    /// <summary>
    /// A ClaimAction that deletes all claims from the given ClaimsIdentity with the given ClaimType.
    /// </summary>
    public class DeleteClaimAction : ClaimAction
    {
        /// <summary>
        /// Creates a new DeleteClaimAction.
        /// </summary>
        /// <param name="claimType">The ClaimType of Claims to delete.</param>
        public DeleteClaimAction(string claimType)
            : base(claimType, ClaimValueTypes.String)
        {
        }

        /// <inheritdoc />
        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
        {
            foreach (var claim in identity.FindAll(ClaimType).ToList())
            {
                identity.TryRemoveClaim(claim);
            }
        }
    }
}
