// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims
{
    /// <summary>
    /// A ClaimAction that selects the value from the json user data by running the given Func resolver.
    /// </summary>
    public class CustomJsonClaimAction : ClaimAction
    {
        /// <summary>
        /// Creates a new CustomJsonClaimAction.
        /// </summary>
        /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
        /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
        /// <param name="resolver">The Func that will be called to select value from the given json user data.</param>
        public CustomJsonClaimAction(string claimType, string valueType, Func<JsonElement, string> resolver)
            : base(claimType, valueType)
        {
            Resolver = resolver;
        }

        /// <summary>
        /// The Func that will be called to select value from the given json user data.
        /// </summary>
        public Func<JsonElement, string> Resolver { get; }

        /// <inheritdoc />
        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
        {
            var value = Resolver(userData);
            if (!string.IsNullOrEmpty(value))
            {
                identity.AddClaim(new Claim(ClaimType, value, ValueType, issuer));
            }
        }
    }
}
