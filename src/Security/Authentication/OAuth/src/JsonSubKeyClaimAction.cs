// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims
{
    /// <summary>
    /// A ClaimAction that selects a second level value from the json user data with the given top level key
    /// name and second level sub key name and add it as a Claim.
    /// This no-ops if the keys are not found or the value is empty.
    /// </summary>
    public class JsonSubKeyClaimAction : JsonKeyClaimAction
    {
        /// <summary>
        /// Creates a new JsonSubKeyClaimAction.
        /// </summary>
        /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
        /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
        /// <param name="jsonKey">The top level key to look for in the json user data.</param>
        /// <param name="subKey">The second level key to look for in the json user data.</param>
        public JsonSubKeyClaimAction(string claimType, string valueType, string jsonKey, string subKey)
            : base(claimType, valueType, jsonKey)
        {
            SubKey = subKey;
        }

        /// <summary>
        /// The second level key to look for in the json user data.
        /// </summary>
        public string SubKey { get; }

        /// <inheritdoc />
        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
        {
            var value = GetValue(userData, JsonKey, SubKey);
            if (!string.IsNullOrEmpty(value))
            {
                identity.AddClaim(new Claim(ClaimType, value, ValueType, issuer));
            }
        }

        // Get the given subProperty from a property.
        private static string GetValue(JsonElement userData, string propertyName, string subProperty)
        {
            if (userData.TryGetProperty(propertyName, out var value)
                && value.ValueKind == JsonValueKind.Object && value.TryGetProperty(subProperty, out value))
            {
                return value.ToString();
            }
            return null;
        }
    }
}
