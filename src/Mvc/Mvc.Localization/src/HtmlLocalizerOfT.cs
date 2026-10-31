// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization;

/// <summary>
/// An <see cref="IHtmlLocalizer"/> implementation that provides localized HTML content for the specified type
/// <typeparamref name="TResource"/>.
/// </summary>
/// <typeparam name="TResource">The <see cref="Type"/> to scope the resource names.</typeparam>
public class HtmlLocalizer<TResource> : IHtmlLocalizer<TResource>
{
    private readonly IHtmlLocalizer _localizer;

    /// <summary>
    /// Creates a new <see cref="HtmlLocalizer{TResource}"/>.
    /// </summary>
    /// <param name="factory">The <see cref="IHtmlLocalizerFactory"/>.</param>
    public HtmlLocalizer(IHtmlLocalizerFactory factory)
    {
        _localizer = factory.Create(typeof(TResource));
    }

    /// <inheritdoc />
    public virtual LocalizedHtmlString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            return _localizer[name];
        }
    }

    /// <inheritdoc />
    public virtual LocalizedHtmlString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            return _localizer[name, arguments];
        }
    }

    /// <inheritdoc />
    public virtual LocalizedString GetString(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _localizer.GetString(name);
    }

    /// <inheritdoc />
    public virtual LocalizedString GetString(string name, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _localizer.GetString(name, arguments);
    }

    /// <inheritdoc />
    public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _localizer.GetAllStrings(includeParentCultures);
}
