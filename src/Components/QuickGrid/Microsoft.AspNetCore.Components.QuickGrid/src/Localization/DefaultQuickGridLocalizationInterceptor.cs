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
    
    /// <summary>
    /// When true, ignore the built-in English resources and prefer custom localizers.
    /// When false (default), built-in English resources are used when available.
    /// </summary>
    public bool IgnoreDefaultEnglish { get; init; }

    public DefaultQuickGridLocalizationInterceptor(ResourceManager resourceManager, QuickGridLocalizer? customLocalizer = null)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _customLocalizer = customLocalizer;
    }

    public override Microsoft.Extensions.Localization.LocalizedString Handle(string key, params object?[]? arguments)
    {
        var args = arguments ?? Array.Empty<object?>();

        var currentLang = CultureInfo.CurrentUICulture.Parent.TwoLetterISOLanguageName;

        if (!IgnoreDefaultEnglish)
        {
            if (_customLocalizer is null || currentLang.Equals("en", StringComparison.InvariantCultureIgnoreCase))
            {
                return GetFromResourceManager(key, args);
            }
        }

        // If a custom localizer exists, try it first for non-English (or when default is ignored).
        if (_customLocalizer is not null)
        {
            var fromCustom = _customLocalizer[key, args];
            if (!fromCustom.ResourceNotFound)
            {
                return new Microsoft.Extensions.Localization.LocalizedString(fromCustom.Name, fromCustom.Value, fromCustom.ResourceNotFound);
            }
        }

        // Fallback to built-in resources.
        return GetFromResourceManager(key, args);
    }

    private Microsoft.Extensions.Localization.LocalizedString GetFromResourceManager(string key, object?[] args)
    {
        var value = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
        if (value is null)
        {
            return new Microsoft.Extensions.Localization.LocalizedString(key, key, resourceNotFound: true);
        }

        string formatted;
        try
        {
            formatted = (args.Length > 0) ? string.Format(CultureInfo.CurrentCulture, value, args) : value;
        }
        catch
        {
            formatted = value;
        }
        return new Microsoft.Extensions.Localization.LocalizedString(key, formatted, resourceNotFound: false);
    }
}
