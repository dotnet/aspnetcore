// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    /// <summary>
    /// Test the <see cref="HtmlHelperValueExtensions" /> class.
    /// </summary>
    public class HtmlHelperValueExtensionsTest
    {
        // Value

        [Fact]
        public void ValueGetsValueFromViewData()
        {
            // Arrange
            var helper = GetHtmlHelper();

            // Act
            var html = helper.Value("StringProperty");

            // Assert
            Assert.Equal("ViewDataValue", html);
        }

        // ValueFor

        [Fact]
        public void ValueForGetsExpressionValueFromViewDataModel()
        {
            // Arrange
            var helper = GetHtmlHelper();

            // Act
            var html = helper.ValueFor(m => m.StringProperty);

            // Assert
            Assert.Equal("ModelStringPropertyValue", html);
        }

        // All Value Helpers including ValueForModel

        [Fact]
        public void ValueHelpersWithErrorsGetValueFromModelState()
        {
            // Arrange
            var model = new TestModel()
            {
                StringProperty = "ModelStringPropertyValue",
                ObjectProperty = "ModelObjectPropertyValue",
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<TestModel>(model);
            var viewData = helper.ViewData;
            viewData["StringProperty"] = "ViewDataValue";
            viewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            var modelState = new ModelState();
            modelState.Value = new ValueProviderResult(
                rawValue: new string[] { "StringPropertyRawValue" },
                attemptedValue: "StringPropertyAttemptedValue",
                culture: CultureInfo.InvariantCulture);
            viewData.ModelState["FieldPrefix.StringProperty"] = modelState;

            modelState = new ModelState();
            modelState.Value = new ValueProviderResult(
                rawValue: new string[] { "ModelRawValue" },
                attemptedValue: "ModelAttemptedValue",
                culture: CultureInfo.InvariantCulture);
            viewData.ModelState["FieldPrefix"] = modelState;

            // Act & Assert
            Assert.Equal("StringPropertyRawValue", helper.Value("StringProperty"));
            Assert.Equal("StringPropertyRawValue", helper.ValueFor(m => m.StringProperty));
            Assert.Equal("ModelRawValue", helper.ValueForModel());
        }

        [Fact]
        [ReplaceCulture]
        public void ValueHelpersWithEmptyNameConvertModelValueUsingCurrentCulture()
        {
            // Arrange
            var helper = GetHtmlHelper();
            var expectedModelValue =
                "{ StringProperty = ModelStringPropertyValue, ObjectProperty = 01/01/1900 00:00:00 }";

            // Act & Assert
            Assert.Equal(expectedModelValue, helper.Value(expression: string.Empty));
            Assert.Equal(expectedModelValue, helper.Value(expression: null)); // null is another alias for current model
            Assert.Equal(expectedModelValue, helper.ValueFor(m => m));
            Assert.Equal(expectedModelValue, helper.ValueForModel());
        }

        [Fact]
        [ReplaceCulture]
        public void ValueHelpersFormatValue()
        {
            // Arrange
            var helper = GetHtmlHelper();
            var expectedModelValue =
                "-{ StringProperty = ModelStringPropertyValue, ObjectProperty = 01/01/1900 00:00:00 }-";
            var expectedObjectPropertyValue = "-01/01/1900 00:00:00-";

            // Act & Assert
            Assert.Equal(expectedModelValue, helper.ValueForModel("-{0}-"));
            Assert.Equal(expectedObjectPropertyValue, helper.Value("ObjectProperty", "-{0}-"));
            Assert.Equal(expectedObjectPropertyValue, helper.ValueFor(m => m.ObjectProperty, "-{0}-"));
        }

        [Fact]
        public void ValueHelpersDoNotEncodeValue()
        {
            // Arrange
            var model = new TestModel { StringProperty = "ModelStringPropertyValue <\"\">" };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<TestModel>(model);
            var viewData = helper.ViewData;
            viewData["StringProperty"] = "ViewDataValue <\"\">";

            var modelState = new ModelState();
            modelState.Value = new ValueProviderResult(
                rawValue: new string[] { "ObjectPropertyRawValue <\"\">" },
                attemptedValue: "ObjectPropertyAttemptedValue <\"\">",
                culture: CultureInfo.InvariantCulture);
            viewData.ModelState["ObjectProperty"] = modelState;

            // Act & Assert
            Assert.Equal(
                "<{ StringProperty = ModelStringPropertyValue <\"\">, ObjectProperty = (null) }>",
                helper.ValueForModel("<{0}>"));
            Assert.Equal("<ViewDataValue <\"\">>", helper.Value("StringProperty", "<{0}>"));
            Assert.Equal("<ModelStringPropertyValue <\"\">>", helper.ValueFor(m => m.StringProperty, "<{0}>"));
            Assert.Equal("ObjectPropertyRawValue <\"\">", helper.ValueFor(m => m.ObjectProperty));
        }

        private sealed class TestModel
        {
            public string StringProperty { get; set; }
            public object ObjectProperty { get; set; }

            public override string ToString()
            {
                return string.Format(
                    "{{ StringProperty = {0}, ObjectProperty = {1} }}",
                    StringProperty ?? "(null)",
                    ObjectProperty ?? "(null)");
            }
        }

        private static HtmlHelper<TestModel> GetHtmlHelper()
        {
            var model = new TestModel
            {
                StringProperty = "ModelStringPropertyValue",
                ObjectProperty = new DateTime(1900, 1, 1, 0, 0, 0),
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<TestModel>(model);
            helper.ViewData["StringProperty"] = "ViewDataValue";

            return helper;
        }
    }
}