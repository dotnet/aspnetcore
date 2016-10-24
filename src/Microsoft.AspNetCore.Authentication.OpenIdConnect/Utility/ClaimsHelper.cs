// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    internal static class ClaimsHelper
    {
        public static void AddClaimsToIdentity(
            JObject userInformationPayload,
            ClaimsIdentity identity,
            string issuer)
        {
            foreach (var pair in userInformationPayload)
            {
                var array = pair.Value as JArray;
                if (array != null)
                {
                    foreach (var item in array)
                    {
                        AddClaimsToIdentity(item, identity, pair.Key, issuer);
                    }
                }
                else
                {
                    AddClaimsToIdentity(pair.Value, identity, pair.Key, issuer);
                }
            }
        }

        private static void AddClaimsToIdentity(JToken item, ClaimsIdentity identity, string key, string issuer)
            => identity.AddClaim(new Claim(key, item?.ToString() ?? string.Empty, GetClaimValueType(item), issuer));

        private static string GetClaimValueType(JToken token)
        {
            if (token == null)
            {
                return JsonClaimValueTypes.JsonNull;
            }

            switch (token.Type)
            {
                case JTokenType.Array:
                    return JsonClaimValueTypes.JsonArray;

                case JTokenType.Boolean:
                    return ClaimValueTypes.Boolean;

                case JTokenType.Date:
                    return ClaimValueTypes.DateTime;

                case JTokenType.Float:
                    return ClaimValueTypes.Double;

                case JTokenType.Integer:
                {
                    var value = (long) token;
                    if (value >= int.MinValue && value <= int.MaxValue)
                    {
                        return ClaimValueTypes.Integer;
                    }

                    return ClaimValueTypes.Integer64;
                }

                case JTokenType.Object:
                    return JsonClaimValueTypes.Json;

                case JTokenType.String:
                    return ClaimValueTypes.String;
            }

            // Fall back to ClaimValueTypes.String when no appropriate
            // claim value type can be inferred from the claim value.
            return ClaimValueTypes.String;
        }
    }
}
