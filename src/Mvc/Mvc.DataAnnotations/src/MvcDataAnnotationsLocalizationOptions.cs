// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// Provides programmatic configuration for DataAnnotations localization in the MVC framework.
/// </summary>
public class MvcDataAnnotationsLocalizationOptions : IEnumerable<ICompatibilitySwitch>
{
    private readonly IReadOnlyList<ICompatibilitySwitch> _switches = Array.Empty<ICompatibilitySwitch>();

    /// <summary>
    /// The delegate to invoke for creating <see cref="IStringLocalizer"/>.
    /// </summary>
    public Func<Type, IStringLocalizerFactory, IStringLocalizer> DataAnnotationLocalizerProvider = null!;

    IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator() => _switches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
}
