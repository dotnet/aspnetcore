// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims
{
    /// <summary>
    /// Infrastructure for mapping user data from a json structure to claims on the ClaimsIdentity.
    /// </summary>
    public abstract class ClaimAction
    {
        /// <summary>
        /// Create a new claim manipulation action.
        /// </summary>
        /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
        /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
        public ClaimAction(string claimType, string valueType)
        {
            ClaimType = claimType;
            ValueType = valueType;
        }

        /// <summary>
        /// The value to use for Claim.Type when creating a Claim.
        /// </summary>
        public string ClaimType { get; }

        // The value to use for Claim.ValueType when creating a Claim.
        public string ValueType { get; }

        /// <summary>
        /// Examine the given userData json, determine if the requisite data is present, and optionally add it
        /// as a new Claim on the ClaimsIdentity.
        /// </summary>
        /// <param name="userData">The source data to examine. This value may be null.</param>
        /// <param name="identity">The identity to add Claims to.</param>
        /// <param name="issuer">The value to use for Claim.Issuer when creating a Claim.</param>
        public abstract void Run(JObject userData, ClaimsIdentity identity, string issuer);
    }
}
