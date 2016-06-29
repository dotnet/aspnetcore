// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class HtmlHelperTextBoxTest
    {
        [Theory]
        [InlineData("text")]
        [InlineData("search")]
        [InlineData("url")]
        [InlineData("tel")]
        [InlineData("email")]
        [InlineData("number")]
        public void TextBoxFor_GeneratesPlaceholderAttribute_WhenDisplayAttributePromptIsSetAndTypeIsValid(string type)
        {
            // Arrange            
            var model = new TextBoxModel();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var textBox = helper.TextBoxFor(m => m.Property1, new { type });

            // Assert 
            var result = HtmlContentUtilities.HtmlContentToString(textBox);
            Assert.Contains(@"placeholder=""HtmlEncode[[placeholder]]""", result, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("hidden")]
        [InlineData("date")]
        [InlineData("time")]
        [InlineData("range")]
        [InlineData("color")]
        [InlineData("checkbox")]
        [InlineData("radio")]
        [InlineData("submit")]
        [InlineData("reset")]
        [InlineData("button")]
        [InlineData("image")]
        [InlineData("file")]
        public void TextBoxFor_DoesNotGeneratePlaceholderAttribute_WhenDisplayAttributePromptIsSetAndTypeIsInvalid(string type)
        {
            // Arrange            
            var model = new TextBoxModel();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var textBox = helper.TextBoxFor(m => m.Property1, new { type });

            // Assert 
            var result = HtmlContentUtilities.HtmlContentToString(textBox);
            Assert.DoesNotContain(@"placeholder=""HtmlEncode[[placeholder]]""", result, StringComparison.Ordinal);
        }

        private class TextBoxModel
        {
            [Display(Prompt = "placeholder")]
            public string Property1 { get; set; }
        }
    }
}