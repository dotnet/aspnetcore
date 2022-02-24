// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Provides configuration for Razor Pages.
/// </summary>
public class RazorPagesOptions : IEnumerable<ICompatibilitySwitch>
{
    private readonly IReadOnlyList<ICompatibilitySwitch> _switches = Array.Empty<ICompatibilitySwitch>();
    private string _root = "/Pages";

    /// <summary>
    /// Gets a collection of <see cref="IPageConvention"/> instances that are applied during
    /// route and page model construction.
    /// </summary>
    public PageConventionCollection Conventions { get; internal set; } = new();

    /// <summary>
    /// Application relative path used as the root of discovery for Razor Page files.
    /// Defaults to the <c>/Pages</c> directory under application root.
    /// </summary>
    public string RootDirectory
    {
        get => _root;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(value));
            }

            if (value[0] != '/')
            {
                throw new ArgumentException(Resources.PathMustBeRootRelativePath, nameof(value));
            }

            _root = value;
        }
    }

    IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator() => _switches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
}
