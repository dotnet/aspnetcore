// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal class TagHelperInfo
{
    public TagHelperInfo(
        string tagName,
        TagMode tagMode,
        TagHelperBinding bindingResult)
    {
        TagName = tagName;
        TagMode = tagMode;
        BindingResult = bindingResult;
    }

    public string TagName { get; }

    public TagMode TagMode { get; }

    public TagHelperBinding BindingResult { get; }
}
