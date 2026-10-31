// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Controls the rendering of hidden input fields when using CheckBox tag helpers or html helpers.
/// </summary>
public enum CheckBoxHiddenInputRenderMode
{
    /// <summary>
    /// Hidden input fields will not be automatically rendered. If checkbox is not checked, no value will be posted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Hidden input fields will be rendered inline with each checkbox. Use this for legacy ASP.NET MVC behavior.
    /// </summary>
    Inline = 1,

    /// <summary>
    /// Hidden input fields will be rendered for each checkbox at the bottom of the form element. This is the preferred render method and default MVC behavior.
    /// If <see cref="Microsoft.AspNetCore.Mvc.ViewFeatures.FormContext.CanRenderAtEndOfForm"/> is <c>false</c>, will fall back on <see cref="Inline"/>.
    /// </summary>
    EndOfForm = 2
}
