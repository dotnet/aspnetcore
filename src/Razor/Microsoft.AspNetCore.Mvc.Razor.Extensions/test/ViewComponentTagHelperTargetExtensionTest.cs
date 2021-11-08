// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class ViewComponentTagHelperTargetExtensionTest
{
    [Fact]
    public void WriteViewComponentTagHelper_GeneratesViewComponentTagHelper()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder
            .Create(ViewComponentTagHelperConventions.Kind, "TestTagHelper", "TestAssembly")
            .TypeName("__Generated__TagCloudViewComponentTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Int32")
                .PropertyName("Foo"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("tagcloud"))
            .AddMetadata(ViewComponentTagHelperMetadata.Name, "TagCloud")
            .Build();

        var extension = new ViewComponentTagHelperTargetExtension();
        var context = TestCodeRenderingContext.CreateRuntime();
        var node = new ViewComponentTagHelperIntermediateNode()
        {
            ClassName = "__Generated__TagCloudViewComponentTagHelper",
            TagHelper = tagHelper
        };

        // Act
        extension.WriteViewComponentTagHelper(context, node);

        // Assert
        var csharp = context.CodeWriter.GenerateCode();
        Assert.Equal(
            @"[Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(""tagcloud"")]
public class __Generated__TagCloudViewComponentTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
{
    private readonly global::Microsoft.AspNetCore.Mvc.IViewComponentHelper __helper = null;
    public __Generated__TagCloudViewComponentTagHelper(global::Microsoft.AspNetCore.Mvc.IViewComponentHelper helper)
    {
        __helper = helper;
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute, global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
    public global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; set; }
    public System.Int32 Foo { get; set; }
    public override async global::System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext __context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput __output)
    {
        (__helper as global::Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware)?.Contextualize(ViewContext);
        var __helperContent = await __helper.InvokeAsync(""TagCloud"", ProcessInvokeAsyncArgs(__context));
        __output.TagName = null;
        __output.Content.SetHtmlContent(__helperContent);
    }
    private Dictionary<string, object> ProcessInvokeAsyncArgs(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext __context)
    {
        Dictionary<string, object> args = new Dictionary<string, object>();
        if (__context.AllAttributes.ContainsName(""Foo""))
        {
            args[nameof(Foo)] = Foo;
        }
        return args;
    }
}
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteViewComponentTagHelper_GeneratesViewComponentTagHelper_WithIndexer()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder
            .Create(ViewComponentTagHelperConventions.Kind, "TestTagHelper", "TestAssembly")
            .TypeName("__Generated__TagCloudViewComponentTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                .PropertyName("Tags")
                .AsDictionaryAttribute("foo-", "System.Int32"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("tagcloud"))
            .AddMetadata(ViewComponentTagHelperMetadata.Name, "TagCloud")
            .Build();

        var extension = new ViewComponentTagHelperTargetExtension();
        var context = TestCodeRenderingContext.CreateRuntime();
        var node = new ViewComponentTagHelperIntermediateNode()
        {
            ClassName = "__Generated__TagCloudViewComponentTagHelper",
            TagHelper = tagHelper
        };

        // Act
        extension.WriteViewComponentTagHelper(context, node);

        // Assert
        var csharp = context.CodeWriter.GenerateCode();
        Assert.Equal(
            @"[Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(""tagcloud"")]
public class __Generated__TagCloudViewComponentTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
{
    private readonly global::Microsoft.AspNetCore.Mvc.IViewComponentHelper __helper = null;
    public __Generated__TagCloudViewComponentTagHelper(global::Microsoft.AspNetCore.Mvc.IViewComponentHelper helper)
    {
        __helper = helper;
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute, global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
    public global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; set; }
    public System.Collections.Generic.Dictionary<System.String, System.Int32> Tags { get; set; }
     = new System.Collections.Generic.Dictionary<System.String, System.Int32>();
    public override async global::System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext __context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput __output)
    {
        (__helper as global::Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware)?.Contextualize(ViewContext);
        var __helperContent = await __helper.InvokeAsync(""TagCloud"", ProcessInvokeAsyncArgs(__context));
        __output.TagName = null;
        __output.Content.SetHtmlContent(__helperContent);
    }
    private Dictionary<string, object> ProcessInvokeAsyncArgs(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext __context)
    {
        Dictionary<string, object> args = new Dictionary<string, object>();
        if (__context.AllAttributes.ContainsName(""Foo""))
        {
            args[nameof(Tags)] = Tags;
        }
        return args;
    }
}
",
            csharp,
            ignoreLineEndingDifferences: true);
    }
}
