// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Test the <see cref="IHtmlHelper.DisplayText"/> and
    /// <see cref="IHtmlHelper{TModel}.DisplayTextFor{TValue}"/> methods.
    /// </summary>
    public class HtmlHelperDisplayTextTest
    {
        [Fact]
        public void DisplayText_ReturnsEmpty_IfValueNull()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: null);

            // Act
            var result = helper.DisplayText(name: string.Empty);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void DisplayTextFor_ReturnsEmpty_IfValueNull()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: null);

            // Act
            var result = helper.DisplayTextFor(m => m);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void DisplayText_ReturnsNullDisplayText_IfSetAndValueNull()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: null);
            helper.ViewData.ModelMetadata.NullDisplayText = "Null display Text";

            // Act
            var result = helper.DisplayText(name: string.Empty);

            // Assert
            Assert.Equal("Null display Text", result);
        }

        [Fact]
        public void DisplayTextFor_ReturnsNullDisplayText_IfSetAndValueNull()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: null);
            helper.ViewData.ModelMetadata.NullDisplayText = "Null display Text";

            // Act
            var result = helper.DisplayTextFor(m => m);

            // Assert
            Assert.Equal("Null display Text", result);
        }

        [Fact]
        public void DisplayText_ReturnsValue_IfNameEmpty()
        {
            // Arrange
            var model = new OverriddenToStringModel("Model value");
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = helper.DisplayText(name: string.Empty);
            var nullResult = helper.DisplayText(name: null);    // null is another alias for current model

            // Assert
            Assert.Equal("Model value", result);
            Assert.Equal("Model value", nullResult);
        }

        [Fact]
        public void DisplayText_ReturnsEmpty_IfNameNotFound()
        {
            // Arrange
            var model = new OverriddenToStringModel("Model value");
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = helper.DisplayText("NonExistentProperty");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void DisplayTextFor_ReturnsValue_IfIdentityExpression()
        {
            // Arrange
            var model = new OverriddenToStringModel("Model value");
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = helper.DisplayTextFor(m => m);

            // Assert
            Assert.Equal("Model value", result);
        }

        [Fact]
        public void DisplayText_ReturnsSimpleDisplayText_IfSetAndValueNonNull()
        {
            // Arrange
            var model = new OverriddenToStringModel("Ignored text");
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            helper.ViewData.ModelMetadata.SimpleDisplayText = "Simple display text";

            // Act
            var result = helper.DisplayText(name: string.Empty);

            // Assert
            Assert.Equal("Simple display text", result);
        }

        [Fact]
        public void DisplayTextFor_ReturnsSimpleDisplayText_IfSetAndValueNonNull()
        {
            // Arrange
            var model = new OverriddenToStringModel("Ignored text");
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            helper.ViewData.ModelMetadata.SimpleDisplayText = "Simple display text";

            // Act
            var result = helper.DisplayTextFor(m => m);

            // Assert
            Assert.Equal("Simple display text", result);
        }

        [Fact]
        public void DisplayText_ReturnsPropertyValue_IfNameFound()
        {
            // Arrange
            var model = new OverriddenToStringModel("Ignored text")
            {
                Name = "Property value",
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            helper.ViewData.ModelMetadata.SimpleDisplayText = "Simple display text";

            // Act
            var result = helper.DisplayText("Name");

            // Assert
            Assert.Equal("Property value", result);
        }

        [Fact]
        public void DisplayTextFor_ReturnsPropertyValue_IfPropertyExpression()
        {
            // Arrange
            var model = new OverriddenToStringModel("ignored text")
            {
                Name = "Property value",
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = helper.DisplayTextFor(m => m.Name);

            // Assert
            Assert.Equal("Property value", result);
        }

        [Fact]
        public void DisplayText_ReturnsViewDataEntry()
        {
            // Arrange
            var model = new OverriddenToStringModel("Model value")
            {
                Name = "Property value",
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            helper.ViewData["Name"] = "View data dictionary value";

            // Act
            var result = helper.DisplayText("Name");

            // Assert
            Assert.Equal("View data dictionary value", result);
        }

        [Fact]
        public void DisplayTextFor_IgnoresViewDataEntry()
        {
            // Arrange
            var model = new OverriddenToStringModel("Model value")
            {
                Name = "Property value",
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            helper.ViewData["Name"] = "View data dictionary value";

            // Act
            var result = helper.DisplayTextFor(m => m.Name);

            // Assert
            Assert.Equal("Property value", result);
        }

        [Fact]
        public void DisplayText_ReturnsModelStateEntry()
        {
            // Arrange
            var model = new OverriddenToStringModel("Model value")
            {
                Name = "Property value",
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var viewData = helper.ViewData;
            viewData["Name"] = "View data dictionary value";
            viewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            var modelState = new ModelState();
            modelState.Value = new ValueProviderResult(
                rawValue: new string[] { "Attempted name value" },
                attemptedValue: "Attempted name value",
                culture: CultureInfo.InvariantCulture);
            viewData.ModelState["FieldPrefix.Name"] = modelState;

            // Act
            var result = helper.DisplayText("Name");

            // Assert
            Assert.Equal("View data dictionary value", result);
        }

        [Fact]
        public void DisplayTextFor_IgnoresModelStateEntry()
        {
            // Arrange
            var model = new OverriddenToStringModel("Model value")
            {
                Name = "Property value",
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var viewData = helper.ViewData;
            viewData["Name"] = "View data dictionary value";
            viewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            var modelState = new ModelState();
            modelState.Value = new ValueProviderResult(
                rawValue: new string[] { "Attempted name value" },
                attemptedValue: "Attempted name value",
                culture: CultureInfo.InvariantCulture);
            viewData.ModelState["FieldPrefix.Name"] = modelState;

            // Act
            var result = helper.DisplayTextFor(m => m.Name);

            // Assert
            Assert.Equal("Property value", result);
        }

        // ModelMetadata.SimpleDisplayText returns ToString() result if that method has been overridden.
        private sealed class OverriddenToStringModel
        {
            private readonly string _simpleDisplayText;

            public OverriddenToStringModel(string simpleDisplayText)
            {
                _simpleDisplayText = simpleDisplayText;
            }

            public string Name { get; set; }

            public override string ToString()
            {
                return _simpleDisplayText;
            }
        }
    }
}