// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Specifies the localized view format for <see cref="LanguageViewLocationExpander"/>.
/// </summary>
public enum LanguageViewLocationExpanderFormat
{
    /// <summary>
    /// Locale is a subfolder under which the view exists.
    /// </summary>
    /// <example>
    /// Home/Views/en-US/Index.chtml
    /// </example>
    SubFolder,

    /// <summary>
    /// Locale is part of the view name as a suffix.
    /// </summary>
    /// <example>
    /// Home/Views/Index.en-US.chtml
    /// </example>
    Suffix
}
