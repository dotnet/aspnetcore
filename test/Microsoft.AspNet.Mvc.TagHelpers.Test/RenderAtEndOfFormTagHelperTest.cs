// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class RenderAtEndOfFormTagHelperTest
    {
        public static TheoryData RenderAtEndOfFormTagHelperData
        {
            get
            {
                // tagBuilderList, expectedOutput
                return new TheoryData<List<TagBuilder>, string>
                {
                    {
                        new List<TagBuilder>
                        {
                            GetTagBuilder("input", "SomeName", "hidden", "false", TagRenderMode.SelfClosing)
                        },
                        @"<input name=""SomeName"" type=""hidden"" value=""false"" />"
                    },
                    {
                        new List<TagBuilder>
                        {
                            GetTagBuilder("input", "SomeName", "hidden", "false", TagRenderMode.SelfClosing),
                            GetTagBuilder("input", "SomeOtherName", "hidden", "false", TagRenderMode.SelfClosing)
                        },
                        @"<input name=""SomeName"" type=""hidden"" value=""false"" />" +
                        @"<input name=""SomeOtherName"" type=""hidden"" value=""false"" />"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RenderAtEndOfFormTagHelperData))]
        public async Task Process_AddsHiddenInputTag_FromEndOfFormContent(List<TagBuilder> tagBuilderList, string expectedOutput)
        {
            // Arrange
            var viewContext = new ViewContext();
            var tagHelperOutput = new TagHelperOutput(
                tagName: "form",
                attributes: new TagHelperAttributeList());

            var tagHelperContext = new TagHelperContext(
                Enumerable.Empty<IReadOnlyTagHelperAttribute>(),
                new Dictionary<object, object>(),
                "someId",
                (useCachedResult) =>
                {
                    Assert.True(viewContext.FormContext.CanRenderAtEndOfForm);
                    foreach (var item in tagBuilderList)
                    {
                        viewContext.FormContext.EndOfFormContent.Add(item);
                    }

                    return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
                });

            var tagHelper = new RenderAtEndOfFormTagHelper
            {
                ViewContext = viewContext
            };

            // Act
            await tagHelper.ProcessAsync(context: tagHelperContext, output: tagHelperOutput);

            // Assert
            Assert.Equal(expectedOutput, tagHelperOutput.PostContent.GetContent());
        }

        private static TagBuilder GetTagBuilder(string tag, string name, string type, string value, TagRenderMode mode)
        {
            var tagBuilder = new TagBuilder(tag);
            tagBuilder.MergeAttribute("name", name);
            tagBuilder.MergeAttribute("type", type);
            tagBuilder.MergeAttribute("value", value);
            tagBuilder.TagRenderMode = mode;

            return tagBuilder;
        }
    }
}
