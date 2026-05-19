// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides programmatic configuration for views in the MVC framework.
/// </summary>
public class MvcViewOptions : IEnumerable<ICompatibilitySwitch>
{
    private readonly IReadOnlyList<ICompatibilitySwitch> _switches = Array.Empty<ICompatibilitySwitch>();
    private HtmlHelperOptions _htmlHelperOptions = new HtmlHelperOptions();

    /// <summary>
    /// Gets or sets programmatic configuration for the HTML helpers and <see cref="Rendering.ViewContext"/>.
    /// </summary>
    public HtmlHelperOptions HtmlHelperOptions
    {
        get => _htmlHelperOptions;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _htmlHelperOptions = value;
        }
    }

    /// <summary>
    /// Gets a list <see cref="IViewEngine"/>s used by this application.
    /// </summary>
    public IList<IViewEngine> ViewEngines { get; } = new List<IViewEngine>();

    /// <summary>
    /// Gets a list of <see cref="IClientModelValidatorProvider"/> instances.
    /// </summary>
    public IList<IClientModelValidatorProvider> ClientModelValidatorProviders { get; } =
        new List<IClientModelValidatorProvider>();

    IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator() => _switches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
}
