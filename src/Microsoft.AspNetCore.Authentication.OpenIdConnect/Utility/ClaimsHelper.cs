// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            => identity.AddClaim(new Claim(key, item?.ToString() ?? string.Empty, ClaimValueTypes.String, issuer));
    }
}
