// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Used for configuring how form inputs should be rendered with respect to
/// the current locale.
/// </summary>
public enum FormInputRenderMode
{
    /// <summary>
    /// When appropriate, use <see cref="CultureInfo.InvariantCulture"/> to format HTML input element values.
    /// Generate a hidden HTML form input for each value that uses culture-invariant formatting
    /// so model binding logic can parse with the correct culture.
    /// </summary>
    DetectCultureFromInputType = 0,

    /// <summary>
    /// Always use <see cref="CultureInfo.CurrentCulture"/> to format input element values.
    /// </summary>
    AlwaysUseCurrentCulture = 1,
}
