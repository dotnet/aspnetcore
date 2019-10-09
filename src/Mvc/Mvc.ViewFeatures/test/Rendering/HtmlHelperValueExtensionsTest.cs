// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class HtmlHelperValueExtensionsTest
    {
        [Fact]
        public void Value_ReturnsModelValue()
        {
            // Arrange
            var model = new SomeModel { SomeProperty = "ModelValue" };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = helper.Value("SomeProperty");

            // Assert
            Assert.Equal("ModelValue", result);
        }

        [Fact]
        public void ValueFor_ReturnsModelValue()
        {
            // Arrange
            var model = new SomeModel { SomeProperty = "ModelValue" };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = helper.ValueFor(m => m.SomeProperty);

            // Assert
            Assert.Equal("ModelValue", result);
        }

        [Fact]
        public void ValueForModel_ReturnsModelValue()
        {
            // Arrange
            var model = new SomeModel { SomeProperty = "ModelValue" };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = helper.ValueForModel();

            // Assert
            Assert.Equal("{ SomeProperty = ModelValue }", result);
        }

        [Fact]
        public void ValueForModel_ReturnsModelValueWithSpecificFormat()
        {
            // Arrange
            var model = new SomeModel { SomeProperty = "ModelValue" };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = helper.ValueForModel(format: "-{0}-");

            // Assert
            Assert.Equal("-{ SomeProperty = ModelValue }-", result);
        }

        private class SomeModel
        {
            public string SomeProperty { get; set; }

            public override string ToString()
            {
                return string.Format(
                    "{{ SomeProperty = {0} }}", SomeProperty ?? "(null)");
            }
        }
    }
}
