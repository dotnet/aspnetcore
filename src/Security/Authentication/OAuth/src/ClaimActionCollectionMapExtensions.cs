// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Extension methods for <see cref="ClaimActionCollection"/>.
/// </summary>
public static class ClaimActionCollectionMapExtensions
{
    /// <summary>
    /// Select a top level value from the json user data with the given key name and add it as a Claim.
    /// This no-ops if the key is not found or the value is empty.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
    /// <param name="jsonKey">The top level key to look for in the json user data.</param>
    public static void MapJsonKey(this ClaimActionCollection collection, string claimType, string jsonKey)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.MapJsonKey(claimType, jsonKey, ClaimValueTypes.String);
    }

    /// <summary>
    /// Select a top level value from the json user data with the given key name and add it as a Claim.
    /// This no-ops if the key is not found or the value is empty.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
    /// <param name="jsonKey">The top level key to look for in the json user data.</param>
    /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
    public static void MapJsonKey(this ClaimActionCollection collection, string claimType, string jsonKey, string valueType)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.Add(new JsonKeyClaimAction(claimType, valueType, jsonKey));
    }

    /// <summary>
    /// Select a second level value from the json user data with the given top level key name and second level sub key name and add it as a Claim.
    /// This no-ops if the keys are not found or the value is empty.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
    /// <param name="jsonKey">The top level key to look for in the json user data.</param>
    /// <param name="subKey">The second level key to look for in the json user data.</param>
    public static void MapJsonSubKey(this ClaimActionCollection collection, string claimType, string jsonKey, string subKey)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.MapJsonSubKey(claimType, jsonKey, subKey, ClaimValueTypes.String);
    }

    /// <summary>
    /// Select a second level value from the json user data with the given top level key name and second level sub key name and add it as a Claim.
    /// This no-ops if the keys are not found or the value is empty.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
    /// <param name="jsonKey">The top level key to look for in the json user data.</param>
    /// <param name="subKey">The second level key to look for in the json user data.</param>
    /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
    public static void MapJsonSubKey(this ClaimActionCollection collection, string claimType, string jsonKey, string subKey, string valueType)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.Add(new JsonSubKeyClaimAction(claimType, valueType, jsonKey, subKey));
    }

    /// <summary>
    /// Run the given resolver to select a value from the json user data to add as a claim.
    /// This no-ops if the returned value is empty.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
    /// <param name="resolver">The Func that will be called to select value from the given json user data.</param>
    public static void MapCustomJson(this ClaimActionCollection collection, string claimType, Func<JsonElement, string?> resolver)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.MapCustomJson(claimType, ClaimValueTypes.String, resolver);
    }

    /// <summary>
    /// Run the given resolver to select a value from the json user data to add as a claim.
    /// This no-ops if the returned value is empty.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
    /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
    /// <param name="resolver">The Func that will be called to select value from the given json user data.</param>
    public static void MapCustomJson(this ClaimActionCollection collection, string claimType, string valueType, Func<JsonElement, string?> resolver)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.Add(new CustomJsonClaimAction(claimType, valueType, resolver));
    }

    /// <summary>
    /// Clears any current ClaimsActions and maps all values from the json user data as claims, excluding duplicates.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    public static void MapAll(this ClaimActionCollection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.Clear();
        collection.Add(new MapAllClaimsAction());
    }

    /// <summary>
    /// Clears any current ClaimsActions and maps all values from the json user data as claims, excluding the specified types.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="exclusions">The types to exclude.</param>
    public static void MapAllExcept(this ClaimActionCollection collection, params string[] exclusions)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.MapAll();
        collection.DeleteClaims(exclusions);
    }

    /// <summary>
    /// Delete all claims from the given ClaimsIdentity with the given ClaimType.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="claimType">The claim type to delete</param>
    public static void DeleteClaim(this ClaimActionCollection collection, string claimType)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.Add(new DeleteClaimAction(claimType));
    }

    /// <summary>
    /// Delete all claims from the ClaimsIdentity with the given claimTypes.
    /// </summary>
    /// <param name="collection">The <see cref="ClaimActionCollection"/>.</param>
    /// <param name="claimTypes">The claim types to delete.</param>
    public static void DeleteClaims(this ClaimActionCollection collection, params string[] claimTypes)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(claimTypes);

        foreach (var claimType in claimTypes)
        {
            collection.Add(new DeleteClaimAction(claimType));
        }
    }
}
