// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Resources;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Default interceptor: tries custom QuickGridLocalizer, then built-in resources.
/// </summary>
internal sealed class DefaultQuickGridLocalizationInterceptor : AbstractQuickGridLocalizationInterceptor
{
    private readonly ResourceManager _resourceManager;
    private readonly QuickGridLocalizer? _customLocalizer;

    public DefaultQuickGridLocalizationInterceptor(ResourceManager resourceManager, QuickGridLocalizer? customLocalizer = null)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _customLocalizer = customLocalizer;
    }

    public override QuickGridLocalizedString Handle(string key, params object?[]? arguments)
    {
        if (_customLocalizer is not null)
        {
            var fromCustom = _customLocalizer[key, arguments ?? Array.Empty<object?>()];
            if (!fromCustom.ResourceNotFound)
            {
                return fromCustom;
            }
        }
        // Handle a small set of built-in French translations for the sample/demo.
        // This keeps the sample runnable without introducing extra generated resource
        // designer files for satellite .resx deployments.
        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase))
        {
            if (FrenchTranslations.TryGetValue(key, out var frValue))
            {
                var formattedFr = FormatWithCulture(frValue, arguments);
                return new QuickGridLocalizedString(key, formattedFr, resourceNotFound: false);
            }
        }

        // Fallback to ResourceManager for other cultures
        var value = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
        if (value is null)
        {
            return new QuickGridLocalizedString(key, key, resourceNotFound: true);
        }

        string formatted;
        try
        {
            formatted = (arguments is { Length: > 0 }) ? string.Format(CultureInfo.CurrentCulture, value, arguments) : value;
        }
        catch
        {
            formatted = value;
        }

        return new QuickGridLocalizedString(key, formatted, resourceNotFound: false);
    }

    private static string FormatWithCulture(string value, object?[]? args)
    {
        try
        {
            return (args is { Length: > 0 }) ? string.Format(CultureInfo.CurrentCulture, value, args) : value;
        }
        catch
        {
            return value;
        }
    }

    private static readonly Dictionary<string, string> FrenchTranslations = new()
    {
        ["QuickGridPaginatorPageSummary"] = "Page {0} sur {1}",
        ["QuickGridPaginatorFirstPage"] = "Aller à la première page",
        ["QuickGridPaginatorPreviousPage"] = "Aller à la page précédente",
        ["QuickGridPaginatorNextPage"] = "Aller à la page suivante",
        ["QuickGridPaginatorLastPage"] = "Aller à la dernière page",
        ["QuickGridPaginatorTotalItems"] = "{0} éléments",
    };
}
