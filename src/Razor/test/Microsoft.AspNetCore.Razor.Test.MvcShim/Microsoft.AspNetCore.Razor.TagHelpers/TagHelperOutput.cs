// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers;

public class TagHelperOutput : IHtmlContentContainer
{
    public TagHelperOutput(
        string tagName,
        TagHelperAttributeList attributes,
        Func<bool, HtmlEncoder, Task<TagHelperContent>> getChildContentAsync)
    {
    }

    public string TagName { get; set; }

    public TagHelperContent PreElement => null;

    public TagHelperContent PreContent => null;

    public TagHelperContent Content => null;

    public TagHelperContent PostContent => null;

    public TagHelperContent PostElement => null;

    public bool IsContentModified => true;

    public TagMode TagMode { get; set; }

    public TagHelperAttributeList Attributes { get; }

    public void Reinitialize(string tagName, TagMode tagMode)
    {
    }

    public void SuppressOutput()
    {
    }

    public Task<TagHelperContent> GetChildContentAsync()
    {
        throw null;
    }

    public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult)
    {
        throw null;
    }

    public Task<TagHelperContent> GetChildContentAsync(HtmlEncoder encoder)
    {
        throw null;
    }

    public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult, HtmlEncoder encoder)
    {
        throw null;
    }

    void IHtmlContentContainer.CopyTo(IHtmlContentBuilder destination)
    {
    }

    void IHtmlContentContainer.MoveTo(IHtmlContentBuilder destination)
    {
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
    }
}
