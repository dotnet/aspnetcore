// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization;

/// <summary>
/// An <see cref="IHtmlLocalizer"/> that uses the provided <see cref="IStringLocalizer"/> to do HTML-aware
/// localization of content.
/// </summary>
public class HtmlLocalizer : IHtmlLocalizer
{
    private readonly IStringLocalizer _localizer;

    /// <summary>
    /// Creates a new <see cref="HtmlLocalizer"/>.
    /// </summary>
    /// <param name="localizer">The <see cref="IStringLocalizer"/> to read strings from.</param>
    public HtmlLocalizer(IStringLocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        _localizer = localizer;
    }

    /// <inheritdoc />
    public virtual LocalizedHtmlString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            return ToHtmlString(_localizer[name]);
        }
    }

    /// <inheritdoc />
    public virtual LocalizedHtmlString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            return ToHtmlString(_localizer[name], arguments);
        }
    }

    /// <inheritdoc />
    public virtual LocalizedString GetString(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _localizer[name];
    }

    /// <inheritdoc />
    public virtual LocalizedString GetString(string name, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _localizer[name, arguments];
    }

    /// <inheritdoc />
    public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _localizer.GetAllStrings(includeParentCultures);

    /// <summary>
    /// Creates a new <see cref="LocalizedHtmlString"/> for a <see cref="LocalizedString"/>.
    /// </summary>
    /// <param name="result">The <see cref="LocalizedString"/>.</param>
    protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result) =>
        new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound);

    /// <summary>
    /// Creates a new <see cref="LocalizedHtmlString"/> for a <see cref="LocalizedString"/>.
    /// </summary>
    /// <param name="result">The <see cref="LocalizedString"/>.</param>
    /// <param name="arguments">The value arguments which will be used in construting the message.</param>
    protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result, object[] arguments) =>
        new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound, arguments);
}
