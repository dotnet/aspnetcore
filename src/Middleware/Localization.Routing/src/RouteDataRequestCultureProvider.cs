// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Localization.Routing;

/// <summary>
/// Determines the culture information for a request via values in the route data.
/// </summary>
public class RouteDataRequestCultureProvider : RequestCultureProvider
{
    /// <summary>
    /// The key that contains the culture name.
    /// Defaults to "culture".
    /// </summary>
    public string RouteDataStringKey { get; set; } = "culture";

    /// <summary>
    /// The key that contains the UI culture name. If not specified or no value is found,
    /// <see cref="RouteDataStringKey"/> will be used.
    /// Defaults to "ui-culture".
    /// </summary>
    public string UIRouteDataStringKey { get; set; } = "ui-culture";

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string? culture = null;
        string? uiCulture = null;

        if (!string.IsNullOrEmpty(RouteDataStringKey))
        {
            culture = httpContext.GetRouteValue(RouteDataStringKey)?.ToString();
        }

        if (!string.IsNullOrEmpty(UIRouteDataStringKey))
        {
            uiCulture = httpContext.GetRouteValue(UIRouteDataStringKey)?.ToString();
        }

        if (culture == null && uiCulture == null)
        {
            // No values specified for either so no match
            return NullProviderCultureResult;
        }

        if (culture != null && uiCulture == null)
        {
            // Value for culture but not for UI culture so default to culture value for both
            uiCulture = culture;
        }
        else if (culture == null && uiCulture != null)
        {
            // Value for UI culture but not for culture so default to UI culture value for both
            culture = uiCulture;
        }

        var providerResultCulture = new ProviderCultureResult(culture, uiCulture);

        return Task.FromResult<ProviderCultureResult?>(providerResultCulture);
    }
}
