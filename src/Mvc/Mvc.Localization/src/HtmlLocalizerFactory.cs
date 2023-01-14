// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization;

/// <summary>
/// An <see cref="IHtmlLocalizerFactory"/> that creates instances of <see cref="HtmlLocalizer"/> using the
/// registered <see cref="IStringLocalizerFactory"/>.
/// </summary>
public class HtmlLocalizerFactory : IHtmlLocalizerFactory
{
    private readonly IStringLocalizerFactory _factory;

    /// <summary>
    /// Creates a new <see cref="HtmlLocalizerFactory"/>.
    /// </summary>
    /// <param name="localizerFactory">The <see cref="IStringLocalizerFactory"/>.</param>
    public HtmlLocalizerFactory(IStringLocalizerFactory localizerFactory)
    {
        ArgumentNullException.ThrowIfNull(localizerFactory);

        _factory = localizerFactory;
    }

    /// <summary>
    /// Creates an <see cref="HtmlLocalizer"/> using the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="resourceSource">The <see cref="Type"/> to load resources for.</param>
    /// <returns>The <see cref="HtmlLocalizer"/>.</returns>
    public virtual IHtmlLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        return new HtmlLocalizer(_factory.Create(resourceSource));
    }

    /// <summary>
    /// Creates an <see cref="HtmlLocalizer"/> using the specified base name and location.
    /// </summary>
    /// <param name="baseName">The base name of the resource to load strings from.</param>
    /// <param name="location">The location to load resources from.</param>
    /// <returns>The <see cref="HtmlLocalizer"/>.</returns>
    public virtual IHtmlLocalizer Create(string baseName, string location)
    {
        ArgumentNullException.ThrowIfNull(baseName);
        ArgumentNullException.ThrowIfNull(location);

        var localizer = _factory.Create(baseName, location);
        return new HtmlLocalizer(localizer);
    }
}
