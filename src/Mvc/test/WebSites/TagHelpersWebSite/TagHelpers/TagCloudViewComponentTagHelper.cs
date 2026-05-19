// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MvcSample.Web.Components;

[HtmlTargetElement("tag-cloud")]
[ViewComponent(Name = "Tags")]
public class TagCloudViewComponentTagHelper : ITagHelper
{
    private static readonly string[] Tags =
        ("Lorem ipsum dolor sit amet consectetur adipisicing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua" +
         "Ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat Duis aute irure " +
         "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur Excepteur sint occaecat cupidatat" +
         "non proident, sunt in culpa qui officia deserunt mollit anim id est laborum")
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    private readonly HtmlEncoder _htmlEncoder;

    public TagCloudViewComponentTagHelper(HtmlEncoder htmlEncoder)
    {
        _htmlEncoder = htmlEncoder;
    }

    public int Count { get; set; }

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    public int Order { get; } = 0;

    public void Init(TagHelperContext context)
    {
    }

    public async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var result = await InvokeAsync(Count);
        var writer = new StringWriter();

        var viewComponentDescriptor = new ViewComponentDescriptor()
        {
            TypeInfo = typeof(TagCloudViewComponentTagHelper).GetTypeInfo(),
            ShortName = "TagCloudViewComponentTagHelper",
            FullName = "TagCloudViewComponentTagHelper",
        };

        await result.ExecuteAsync(new ViewComponentContext(
            viewComponentDescriptor,
            new Dictionary<string, object>(),
            _htmlEncoder,
            ViewContext,
            writer));

        output.TagName = null;
        output.Content.AppendHtml(writer.ToString());
    }

    public async Task<IViewComponentResult> InvokeAsync(int count)
    {
        var tags = await GetTagsAsync(count);

        return new ContentViewComponentResult(string.Join(",", tags));
    }

    private Task<string[]> GetTagsAsync(int count)
    {
        return Task.FromResult(GetTags(count));
    }

    private string[] GetTags(int count)
    {
        return Tags.Take(count).ToArray();
    }
}
