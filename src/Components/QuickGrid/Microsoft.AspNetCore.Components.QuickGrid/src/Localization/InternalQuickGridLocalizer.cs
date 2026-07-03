// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Resources;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Internal localization entry point for QuickGrid pagination.
/// </summary>
internal sealed class InternalQuickGridLocalizer
{
    private readonly ResourceManager _resourceManager;
    private readonly QuickGridLocalizer? _quickGridLocalizer;

    public InternalQuickGridLocalizer(
        ResourceManager resourceManager,
        QuickGridLocalizer? quickGridLocalizer = null)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);

        _resourceManager = resourceManager;
        _quickGridLocalizer = quickGridLocalizer;
    }

    public string this[string key] => GetString(key);

    public string this[string key, params object[] arguments] => GetString(key, arguments);

    private string GetString(string key, params object[] arguments)
    {
        arguments ??= Array.Empty<object>();

        if (_quickGridLocalizer is not null)
        {
            var customValue = arguments.Length > 0
                ? _quickGridLocalizer[key, arguments]
                : _quickGridLocalizer[key];

            if (!customValue.ResourceNotFound && !string.IsNullOrWhiteSpace(customValue.Value))
            {
                return customValue.Value;
            }
        }

        var defaultValue = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);

        if (string.IsNullOrWhiteSpace(defaultValue))
        {
            return key;
        }

        if (arguments.Length == 0)
        {
            return defaultValue;
        }

        // Malformed .resx templates (e.g. missing {0}/{1} placeholders) would
        // otherwise throw a FormatException and tear down the renderer.
        try
        {
            return string.Format(CultureInfo.CurrentCulture, defaultValue, arguments);
        }
        catch (FormatException)
        {
            return defaultValue;
        }
    }
}
