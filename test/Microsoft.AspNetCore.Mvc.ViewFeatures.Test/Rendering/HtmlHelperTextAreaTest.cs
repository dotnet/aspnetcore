// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class HtmlHelperTextBoxAreaTest
    {
        [Fact]
        public void TextAreaFor_GeneratesPlaceholderAttribute_WhenDisplayAttributePromptIsSetAndTypeIsValid()
        {
            // Arrange            
            var model = new TextAreaModelWithAPlaceholder();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var textArea = helper.TextAreaFor(m => m.Property1);

            // Assert 
            var result = HtmlContentUtilities.HtmlContentToString(textArea);
            Assert.Contains(@"placeholder=""HtmlEncode[[placeholder]]""", result, StringComparison.Ordinal);
        }

        [Fact]
        public void TextAreaFor_DoesNotGeneratePlaceholderAttribute_WhenNoPlaceholderPresentInModel()
        {
            // Arrange            
            var model = new TextAreaModelWithoutAPlaceholder();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var textArea = helper.TextAreaFor(m => m.Property1);

            // Assert 
            var result = HtmlContentUtilities.HtmlContentToString(textArea);
            Assert.DoesNotContain(@"placeholder=""HtmlEncode[[placeholder]]""", result, StringComparison.Ordinal);
        }

        private class TextAreaModelWithAPlaceholder
        {
            [Display(Prompt = "placeholder")]
            public string Property1 { get; set; }
        }

        private class TextAreaModelWithoutAPlaceholder
        {
            public string Property1 { get; set; }
        }
    }
}
