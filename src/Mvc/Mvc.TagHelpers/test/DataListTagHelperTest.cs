// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class DataListTagHelperTest
{
    [Fact]
    public void Process_WithItems_GeneratesExpectedOptions()
    {
        // Arrange
        var selectItems = new SelectList(Enumerable.Range(0, 3));

        var expectedOptions =
            "<option>HtmlEncode[[0]]</option>" + Environment.NewLine +
            "<option>HtmlEncode[[1]]</option>" + Environment.NewLine +
            "<option>HtmlEncode[[2]]</option>" + Environment.NewLine;

        var metadataProvider = new EmptyModelMetadataProvider();
        var generator = new TestableHtmlGenerator(metadataProvider);

        var tagHelper = new DataListTagHelper(generator)
        {
            Items = selectItems,
        };

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "test");

        var output = new TagHelperOutput(
            "datalist",
            new TagHelperAttributeList(),
            (_, __) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));

        // Act
        tagHelper.Process(context, output);

        // Assert
        Assert.Equal(
            expectedOptions,
            HtmlContentUtilities.HtmlContentToString(output.PostContent));

        Assert.Equal("datalist", output.TagName);
    }
}
