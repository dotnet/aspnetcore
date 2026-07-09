// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

using Microsoft.Extensions.Localization;

/// <summary>
/// Contract for advanced localization interception.
/// Applications may implement this to provide custom translation lookup logic for QuickGrid.
/// </summary>
public interface IQuickGridLocalizationInterceptor
{
    /// <summary>
    /// Retrieves the localized string for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The resource key to look up.</param>
    /// <param name="arguments">Optional format arguments.</param>
    /// <returns>A <see cref="LocalizedString"/> representing the translation. If no translation is available, the returned value's <see cref="LocalizedString.ResourceNotFound"/> should be <c>true</c>.</returns>
    LocalizedString Handle(string key, params object?[]? arguments);
}
