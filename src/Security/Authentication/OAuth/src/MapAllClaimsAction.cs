// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims;

/// <summary>
/// A ClaimAction that selects all top level values from the json user data and adds them as Claims.
/// This excludes duplicate sets of names and values.
/// </summary>
public class MapAllClaimsAction : ClaimAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="MapAllClaimsAction"/>.
    /// </summary>
    public MapAllClaimsAction() : base("All", ClaimValueTypes.String)
    {
    }

    /// <inheritdoc />
    public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
    {
        foreach (var pair in userData.EnumerateObject())
        {
            var claimValue = pair.Value.ToString()!;

            // Avoid adding a claim if there's a duplicate name and value. This often happens in OIDC when claims are
            // retrieved both from the id_token and from the user-info endpoint.
            var duplicate = identity.FindFirst(c => string.Equals(c.Type, pair.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(c.Value, claimValue, StringComparison.Ordinal)) != null;

            if (!duplicate)
            {
                identity.AddClaim(new Claim(pair.Name, claimValue, ClaimValueTypes.String, issuer));
            }
        }
    }
}
