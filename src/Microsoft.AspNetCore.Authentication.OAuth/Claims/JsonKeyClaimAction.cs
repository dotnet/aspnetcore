// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims
{
    /// <summary>
    /// A ClaimAction that selects a top level value from the json user data with the given key name and adds it as a Claim.
    /// This no-ops if the key is not found or the value is empty.
    /// </summary>
    public class JsonKeyClaimAction : ClaimAction
    {
        /// <summary>
        /// Creates a new JsonKeyClaimAction.
        /// </summary>
        /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
        /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
        /// <param name="jsonKey">The top level key to look for in the json user data.</param>
        public JsonKeyClaimAction(string claimType, string valueType, string jsonKey)
            : base(claimType, valueType)
        {
            JsonKey = jsonKey;
        }

        /// <summary>
        /// The top level key to look for in the json user data.
        /// </summary>
        public string JsonKey { get; }

        /// <inheritdoc />
        public override void Run(JObject userData, ClaimsIdentity identity, string issuer)
        {
            var value = userData?[JsonKey];
            if (value is JValue)
            {
                AddClaim(value?.ToString(), identity, issuer);
            }
            else if (value is JArray)
            {
                foreach (var v in value)
                {
                    AddClaim(v?.ToString(), identity, issuer);
                }
            }
        }

        private void AddClaim(string value, ClaimsIdentity identity, string issuer)
        {
            if (!string.IsNullOrEmpty(value))
            {
                identity.AddClaim(new Claim(ClaimType, value, ValueType, issuer));
            }
        }
    }
}
