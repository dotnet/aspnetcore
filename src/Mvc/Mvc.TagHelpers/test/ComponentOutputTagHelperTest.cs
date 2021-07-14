// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Test
{
    public class ComponentOutputTagHelperTest
    {
        [Fact]
        public void ProcessAsync_ThrowsIfNameNotFoundInDeferredContentStore()
        {
            // Arrange
            var tagHelper = new ComponentOutputTagHelper
            {
                Name = "UnknownName",
                ViewContext = new()
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => tagHelper.Process(GetTagHelperContext(), GetTagHelperOutput()));
            Assert.Equal("No component has an output name matching 'UnknownName'.", ex.Message);
        }

        [Fact]
        public void ProcessAsync_ReturnsContentStoreEntryIfNameIsValid()
        {
            // Arrange
            var viewContext = new ViewContext();
            var contentStore = ComponentDeferredContentStore.GetOrCreateContentStore(viewContext);
            contentStore.Add("ValidName", new HtmlContentBuilder().AppendHtml("Hello world"));

            var tagHelper = new ComponentOutputTagHelper
            {
                Name = "ValidName",
                ViewContext = viewContext
            };

            var output = GetTagHelperOutput();

            // Act
            tagHelper.Process(GetTagHelperContext(), output);

            // Assert
            var content = HtmlContentUtilities.HtmlContentToString(output.Content);
            Assert.Equal("Hello world", content);
            Assert.Null(output.TagName);
        }

        private static TagHelperContext GetTagHelperContext()
            => new("component-output", new(), new Dictionary<object, object>(), Guid.NewGuid().ToString("N"));

        private static TagHelperOutput GetTagHelperOutput()
            => new("component-output", new(), (_, __) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }
}
