// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if MOCK_SUPPORT
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.TestCommon;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures.Internal;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public class DefaultEditorTemplatesTest
    {
        // Mappings from templateName to expected result when using StubbyHtmlHelper.
        public static TheoryData<string, string> TemplateNameData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    { null, "__TextBox__ class='text-box single-line'" },
                    { string.Empty, "__TextBox__ class='text-box single-line'" },
                    { "EmailAddress", "__TextBox__ class='text-box single-line' type='email'" },
                    { "emailaddress", "__TextBox__ class='text-box single-line' type='email'" },
                    { "HiddenInput", "HtmlEncode[[True]]__Hidden__" }, // Hidden also generates value by default.
                    { "HIDDENINPUT", "HtmlEncode[[True]]__Hidden__" },
                    { "MultilineText", "__TextArea__ class='text-box multi-line'" },
                    { "multilinetext", "__TextArea__ class='text-box multi-line'" },
                    { "Password", "__Password__ class='text-box single-line password'" },
                    { "PASSWORD", "__Password__ class='text-box single-line password'" },
                    { "PhoneNumber", "__TextBox__ class='text-box single-line' type='tel'" },
                    { "phonenumber", "__TextBox__ class='text-box single-line' type='tel'" },
                    { "Text", "__TextBox__ class='text-box single-line'" },
                    { "TEXT", "__TextBox__ class='text-box single-line'" },
                    { "Url", "__TextBox__ class='text-box single-line' type='url'" },
                    { "url", "__TextBox__ class='text-box single-line' type='url'" },
                    { "Date", "__TextBox__ class='text-box single-line' type='date'" },
                    { "DATE", "__TextBox__ class='text-box single-line' type='date'" },
                    { "DateTime", "__TextBox__ class='text-box single-line' type='datetime'" },
                    { "datetime", "__TextBox__ class='text-box single-line' type='datetime'" },
                    { "DateTime-local", "__TextBox__ class='text-box single-line' type='datetime-local'" },
                    { "DATETIME-LOCAL", "__TextBox__ class='text-box single-line' type='datetime-local'" },
                    { "Time", "__TextBox__ class='text-box single-line' type='time'" },
                    { "time", "__TextBox__ class='text-box single-line' type='time'" },
                    { "Byte", "__TextBox__ class='text-box single-line' type='number'" },
                    { "BYTE", "__TextBox__ class='text-box single-line' type='number'" },
                    { "SByte", "__TextBox__ class='text-box single-line' type='number'" },
                    { "sbyte", "__TextBox__ class='text-box single-line' type='number'" },
                    { "Int16", "__TextBox__ class='text-box single-line' type='number'" },
                    { "INT16", "__TextBox__ class='text-box single-line' type='number'" },
                    { "UInt16", "__TextBox__ class='text-box single-line' type='number'" },
                    { "uint16", "__TextBox__ class='text-box single-line' type='number'" },
                    { "Int32", "__TextBox__ class='text-box single-line' type='number'" },
                    { "INT32", "__TextBox__ class='text-box single-line' type='number'" },
                    { "UInt32", "__TextBox__ class='text-box single-line' type='number'" },
                    { "uint32", "__TextBox__ class='text-box single-line' type='number'" },
                    { "Int64", "__TextBox__ class='text-box single-line' type='number'" },
                    { "INT64", "__TextBox__ class='text-box single-line' type='number'" },
                    { "UInt64", "__TextBox__ class='text-box single-line' type='number'" },
                    { "uint64", "__TextBox__ class='text-box single-line' type='number'" },
                    { "Single", "__TextBox__ class='text-box single-line'" },
                    { "SINGLE", "__TextBox__ class='text-box single-line'" },
                    { "Double", "__TextBox__ class='text-box single-line'" },
                    { "double", "__TextBox__ class='text-box single-line'" },
                    { "Boolean", "__CheckBox__ class='check-box'" }, // Not tri-state b/c string is not a Nullable type.
                    { "BOOLEAN", "__CheckBox__ class='check-box'" },
                    { "Decimal", "__TextBox__ class='text-box single-line'" },
                    { "decimal", "__TextBox__ class='text-box single-line'" },
                    { "String", "__TextBox__ class='text-box single-line'" },
                    { "STRING", "__TextBox__ class='text-box single-line'" },
                    { typeof(IFormFile).Name, "__TextBox__ class='text-box single-line' type='file'" },
                    { TemplateRenderer.IEnumerableOfIFormFileName,
                        "__TextBox__ class='text-box single-line' type='file' multiple='multiple'" },
                };
            }
        }

        [Fact]
        public void ObjectTemplateEditsSimplePropertiesOnObjectByDefault()
        {
            var expected =
                "<div class=\"HtmlEncode[[editor-label]]\"><label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Property1]]</label></div>" + Environment.NewLine
              + "<div class=\"HtmlEncode[[editor-field]]\">Model = p1, ModelType = System.String, PropertyName = Property1," +
                    " SimpleDisplayText = p1 " +
                    "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[true]]\">" +
                    "</span></div>" + Environment.NewLine
              + "<div class=\"HtmlEncode[[editor-label]]\"><label for=\"HtmlEncode[[Property2]]\">HtmlEncode[[Prop2]]</label></div>" + Environment.NewLine
              + "<div class=\"HtmlEncode[[editor-field]]\">Model = (null), ModelType = System.String, PropertyName = Property2," +
                    " SimpleDisplayText = (null) " +
                    "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property2]]\" data-valmsg-replace=\"HtmlEncode[[true]]\">" +
                    "</span></div>" + Environment.NewLine;

            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void ObjectTemplateDisplaysNullDisplayTextWithNullModelAndTemplateDepthGreaterThanOne()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<DefaultTemplatesUtilities.ObjectTemplateModel>().DisplayDetails(dd =>
            {
                dd.NullDisplayText = "Null Display Text";
                dd.SimpleDisplayProperty = "Property1";
            });

            var html = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            html.ViewData.TemplateInfo.AddVisited("foo");
            html.ViewData.TemplateInfo.AddVisited("bar");

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(html.ViewData.ModelMetadata.NullDisplayText, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [MemberData(nameof(DefaultDisplayTemplatesTest.HtmlEncodeData), MemberType = typeof(DefaultDisplayTemplatesTest))]
        public void ObjectTemplateDisplaysSimpleDisplayTextWithNonNullModelTemplateDepthGreaterThanOne(
            string simpleDisplayText,
            bool htmlEncode,
            string expectedResult)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel()
            {
                Property1 = simpleDisplayText,
            };

            var provider = new TestModelMetadataProvider();
            provider.ForType<DefaultTemplatesUtilities.ObjectTemplateModel>().DisplayDetails(dd =>
            {
                dd.HtmlEncode = htmlEncode;
                dd.NullDisplayText = "Null Display Text";
                dd.SimpleDisplayProperty = "Property1";
            });

            var html = DefaultTemplatesUtilities.GetHtmlHelper(model, provider: provider);

            html.ViewData.TemplateInfo.AddVisited("foo");
            html.ViewData.TemplateInfo.AddVisited("bar");

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void ObjectTemplate_IgnoresPropertiesWith_ScaffoldColumnFalse()
        {
            // Arrange
            var expected =
@"<div class=""HtmlEncode[[editor-label]]""><label for=""HtmlEncode[[Property1]]"">HtmlEncode[[Property1]]</label></div>" +
Environment.NewLine +
@"<div class=""HtmlEncode[[editor-field]]""><input class=""HtmlEncode[[text-box single-line]]"" id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[text]]"" value="""" /> " +
@"<span class=""HtmlEncode[[field-validation-valid]]"" data-valmsg-for=""HtmlEncode[[Property1]]"" data-valmsg-replace=""HtmlEncode[[true]]""></span></div>" +
Environment.NewLine +
@"<div class=""HtmlEncode[[editor-label]]""><label for=""HtmlEncode[[Property3]]"">HtmlEncode[[Property3]]</label></div>" +
Environment.NewLine +
@"<div class=""HtmlEncode[[editor-field]]""><input class=""HtmlEncode[[text-box single-line]]"" id=""HtmlEncode[[Property3]]"" name=""HtmlEncode[[Property3]]"" type=""HtmlEncode[[text]]"" value="""" /> " +
@"<span class=""HtmlEncode[[field-validation-valid]]"" data-valmsg-for=""HtmlEncode[[Property3]]"" data-valmsg-replace=""HtmlEncode[[true]]""></span></div>" +
Environment.NewLine;

            var model = new DefaultTemplatesUtilities.ObjectWithScaffoldColumn();
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(htmlHelper);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void ObjectTemplate_HonoursHideSurroundingHtml()
        {
            // Arrange
            var expected =
                "Model = p1, ModelType = System.String, PropertyName = Property1, SimpleDisplayText = p1" +
                "<div class=\"HtmlEncode[[editor-label]]\"><label for=\"HtmlEncode[[Property2]]\">HtmlEncode[[Prop2]]</label></div>" +
                Environment.NewLine +
                "<div class=\"HtmlEncode[[editor-field]]\">" +
                    "Model = (null), ModelType = System.String, PropertyName = Property2, SimpleDisplayText = (null) " +
                    "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property2]]\" data-valmsg-replace=\"HtmlEncode[[true]]\">" +
                    "</span></div>" +
                Environment.NewLine;

            var provider = new TestModelMetadataProvider();
            provider.ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1").DisplayDetails(dd =>
            {
                dd.HideSurroundingHtml = true;
            });

            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model, provider: provider);

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void ObjectTemplate_OrdersProperties_AsExpected()
        {
            // Arrange
            var model = new OrderedModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var expectedProperties = new List<string>
            {
                "OrderedProperty3",
                "OrderedProperty2",
                "OrderedProperty1",
                "Property3",
                "Property1",
                "Property2",
                "LastProperty",
            };

            var stringBuilder = new StringBuilder();
            foreach (var property in expectedProperties)
            {
                var label = string.Format(
                    CultureInfo.InvariantCulture,
                    "<div class=\"HtmlEncode[[editor-label]]\"><label for=\"HtmlEncode[[{0}]]\">HtmlEncode[[{0}]]</label></div>",
                    property);
                stringBuilder.AppendLine(label);

                var value = string.Format(
                    CultureInfo.InvariantCulture,
                    "<div class=\"HtmlEncode[[editor-field]]\">Model = (null), ModelType = System.String, PropertyName = {0}, " +
                    "SimpleDisplayText = (null) " +
                    "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[{0}]]\" data-valmsg-replace=\"HtmlEncode[[true]]\">" +
                    "</span></div>",
                    property);
                stringBuilder.AppendLine(value);
            }
            var expected = stringBuilder.ToString();

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void HiddenInputTemplate_ReturnsValueAndHiddenInput()
        {
            // Arrange
            var expected =
                "HtmlEncode[[Formatted string]]<input id=\"HtmlEncode[[FieldPrefix]]\" name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[Model string]]\" />";

            var model = "Model string";
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var templateInfo = html.ViewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = "FieldPrefix";

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used below.
            templateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = DefaultEditorTemplates.HiddenInputTemplate(html);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void HiddenInputTemplate_HonoursHideSurroundingHtml()
        {
            // Arrange
            var expected = "<input id=\"HtmlEncode[[FieldPrefix]]\" name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[Model string]]\" />";

            var model = "Model string";

            var provider = new TestModelMetadataProvider();
            provider.ForType<string>().DisplayDetails(dd =>
            {
                dd.HideSurroundingHtml = true;
            });

            var html = DefaultTemplatesUtilities.GetHtmlHelper(model, provider: provider);

            var templateInfo = html.ViewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = "FieldPrefix";
            templateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = DefaultEditorTemplates.HiddenInputTemplate(html);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void MultilineTextTemplate_ReturnsTextArea()
        {
            // Arrange
            var expected =
                "<textarea class=\"HtmlEncode[[text-box multi-line]]\" id=\"HtmlEncode[[FieldPrefix]]\" name=\"HtmlEncode[[FieldPrefix]]\">" +
                Environment.NewLine +
                "HtmlEncode[[Formatted string]]</textarea>";

            var model = "Model string";
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var templateInfo = html.ViewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = "FieldPrefix";

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used below.
            templateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = DefaultEditorTemplates.MultilineTemplate(html);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [MemberData(nameof(TemplateNameData))]
        public void Editor_CallsExpectedHtmlHelper(string templateName, string expectedResult)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "True" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                viewEngine.Object,
                innerHelper => new StubbyHtmlHelper(innerHelper));
            helper.ViewData["Property1"] = "True";

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used in most templates.
            helper.ViewData.TemplateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = helper.Editor(
                "Property1",
                templateName,
                htmlFieldName: null,
                additionalViewData: null);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [MemberData(nameof(TemplateNameData))]
        public void EditorFor_CallsExpectedHtmlHelper(string templateName, string expectedResult)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "True" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                viewEngine.Object,
                innerHelper => new StubbyHtmlHelper(innerHelper));

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used in most templates.
            helper.ViewData.TemplateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = helper.EditorFor(
                anotherModel => anotherModel.Property1,
                templateName,
                htmlFieldName: null,
                additionalViewData: null);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [MemberData(nameof(TemplateNameData))]
        public void Editor_CallsExpectedHtmlHelper_DataTypeName(string templateName, string expectedResult)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "True" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));

            var provider = new TestModelMetadataProvider();
            provider.ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1").DisplayDetails(dd =>
            {
                dd.DataTypeName = templateName;
            });

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                Mock.Of<IUrlHelper>(),
                viewEngine.Object,
                provider,
                innerHelper => new StubbyHtmlHelper(innerHelper));
            helper.ViewData["Property1"] = "True";

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used in most templates.
            helper.ViewData.TemplateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = helper.Editor(
                "Property1",
                templateName,
                htmlFieldName: null,
                additionalViewData: null);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [MemberData(nameof(TemplateNameData))]
        public void EditorFor_CallsExpectedHtmlHelper_DataTypeName(string templateName, string expectedResult)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "True" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));

            var provider = new TestModelMetadataProvider();
            provider.ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1").DisplayDetails(dd =>
            {
                dd.DataTypeName = templateName;
            });

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                Mock.Of<IUrlHelper>(),
                viewEngine.Object,
                provider,
                innerHelper => new StubbyHtmlHelper(innerHelper));

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used in most templates.
            helper.ViewData.TemplateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = helper.EditorFor(
                anotherModel => anotherModel.Property1,
                templateName,
                htmlFieldName: null,
                additionalViewData: null);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [MemberData(nameof(TemplateNameData))]
        public void Editor_CallsExpectedHtmlHelper_TemplateHint(string templateName, string expectedResult)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "True" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));

            var provider = new TestModelMetadataProvider();
            provider.ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1").DisplayDetails(dd =>
            {
                dd.TemplateHint = templateName;
            });

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                Mock.Of<IUrlHelper>(),
                viewEngine.Object,
                provider,
                innerHelper => new StubbyHtmlHelper(innerHelper));
            helper.ViewData["Property1"] = "True";

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used in most templates.
            helper.ViewData.TemplateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = helper.Editor(
                "Property1",
                templateName,
                htmlFieldName: null,
                additionalViewData: null);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [MemberData(nameof(TemplateNameData))]
        public void EditorFor_CallsExpectedHtmlHelper_TemplateHint(string templateName, string expectedResult)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "True" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));

            var provider = new TestModelMetadataProvider();
            provider.ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1").DisplayDetails(dd =>
            {
                dd.TemplateHint = templateName;
            });

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                Mock.Of<IUrlHelper>(),
                viewEngine.Object,
                provider,
                innerHelper => new StubbyHtmlHelper(innerHelper));

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used in most templates.
            helper.ViewData.TemplateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = helper.EditorFor(
                anotherModel => anotherModel.Property1,
                templateName,
                htmlFieldName: null,
                additionalViewData: null);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void Editor_FindsViewDataMember()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act
            var result = helper.Editor("Property1");

            // Assert
            Assert.Equal(
                "<input class=\"HtmlEncode[[text-box single-line]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[ViewData string]]\" />",
                HtmlContentUtilities.HtmlContentToString(result));
        }

        // DateTime-local is not special-cased unless using Html5DateRenderingMode.Rfc3339.
        [Theory]
        [InlineData("date", "{0:d}", "02/01/2000")]
        [InlineData("datetime", null, "02/01/2000 03:04:05 +00:00")]
        [InlineData("datetime-local", null, "02/01/2000 03:04:05 +00:00")]
        [InlineData("time", "{0:t}", "03:04")]
        [ReplaceCulture]
        public void Editor_FindsCorrectDateOrTimeTemplate(string dataTypeName, string editFormatString, string expected)
        {
            // Arrange
            // Mono issue - https://github.com/aspnet/External/issues/19
            var expectedInput = PlatformNormalizer.NormalizeContent(
                "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
                "data-val-required=\"HtmlEncode[[The DateTimeOffset field is required.]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
                "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[" +
                dataTypeName + 
                "]]\" value=\"HtmlEncode[[" + expected + "]]\" />");

            var offset = TimeSpan.FromHours(0);
            var model = new DateTimeOffset(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 6,
                offset: offset);
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));

            var provider = new TestModelMetadataProvider();
            provider.ForType<DateTimeOffset>().DisplayDetails(dd =>
            {
                dd.DataTypeName = dataTypeName;
                dd.EditFormatString = editFormatString; // What [DataType] does for given type.
            });

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                Mock.Of<IUrlHelper>(),
                viewEngine.Object,
                provider);
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            // Act
            var result = helper.Editor("");

            // Assert
            Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [InlineData("date", "{0:d}", "2000-01-02")]
        [InlineData("datetime", null, "2000-01-02T03:04:05.060+00:00")]
        [InlineData("datetime-local", null, "2000-01-02T03:04:05.060")]
        [InlineData("time", "{0:t}", "03:04:05.060")]
        [ReplaceCulture]
        public void Editor_AppliesRfc3339(string dataTypeName, string editFormatString, string expected)
        {
            // Arrange
            // Mono issue - https://github.com/aspnet/External/issues/19
            var expectedInput = PlatformNormalizer.NormalizeContent(
                "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
                "data-val-required=\"HtmlEncode[[The DateTimeOffset field is required.]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
                "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[" +
                dataTypeName + 
                "]]\" value=\"HtmlEncode[[" + expected + "]]\" />");

            // Place DateTime-local value in current timezone.
            var offset = string.Equals("", dataTypeName) ? DateTimeOffset.Now.Offset : TimeSpan.FromHours(0);
            var model = new DateTimeOffset(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                offset: offset);
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));

            var provider = new TestModelMetadataProvider();
            provider.ForType<DateTimeOffset>().DisplayDetails(dd =>
            {
                dd.DataTypeName = dataTypeName;
                dd.EditFormatString = editFormatString; // What [DataType] does for given type.
            });

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                Mock.Of<IUrlHelper>(),
                viewEngine.Object,
                provider);
            helper.Html5DateRenderingMode = Html5DateRenderingMode.Rfc3339;
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            // Act
            var result = helper.Editor("");

            // Assert
            Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [InlineData("date", Html5DateRenderingMode.CurrentCulture)]
        [InlineData("date", Html5DateRenderingMode.Rfc3339)]
        [InlineData("datetime", Html5DateRenderingMode.CurrentCulture)]
        [InlineData("datetime", Html5DateRenderingMode.Rfc3339)]
        [InlineData("datetime-local", Html5DateRenderingMode.CurrentCulture)]
        [InlineData("datetime-local", Html5DateRenderingMode.Rfc3339)]
        [InlineData("time", Html5DateRenderingMode.CurrentCulture)]
        [InlineData("time", Html5DateRenderingMode.Rfc3339)]
        public void Editor_AppliesNonDefaultEditFormat(string dataTypeName, Html5DateRenderingMode renderingMode)
        {
            // Arrange
            // Mono issue - https://github.com/aspnet/External/issues/19
            var expectedInput = PlatformNormalizer.NormalizeContent(
                "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
                "data-val-required=\"HtmlEncode[[The DateTimeOffset field is required.]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
                "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[" +
                dataTypeName + 
                "]]\" value=\"HtmlEncode[[Formatted as 2000-01-02T03:04:05.0600000+00:00]]\" />");

            var offset = TimeSpan.FromHours(0);
            var model = new DateTimeOffset(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                offset: offset);
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));


            var provider = new TestModelMetadataProvider();
            provider.ForType<DateTimeOffset>().DisplayDetails(dd =>
            {
                dd.DataTypeName = dataTypeName;
                dd.EditFormatString = "Formatted as {0:O}"; // What [DataType] does for given type.
                dd.HasNonDefaultEditFormat = true;
            });

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(
                model,
                Mock.Of<IUrlHelper>(),
                viewEngine.Object,
                provider);

            helper.Html5DateRenderingMode = renderingMode; // Ignored due to HasNonDefaultEditFormat.
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            // Act
            var result = helper.Editor("");

            // Assert
            Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void EditorFor_FindsModel()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act
            var result = helper.EditorFor(m => m.Property1);

            // Assert
            Assert.Equal(
                "<input class=\"HtmlEncode[[text-box single-line]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[Model string]]\" />",
                HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void Editor_FindsModel_IfNoViewDataMember()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var result = helper.Editor("Property1");

            // Assert
            Assert.Equal(
                "<input class=\"HtmlEncode[[text-box single-line]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[Model string]]\" />",
                HtmlContentUtilities.HtmlContentToString(result));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void EditorFor_FindsModel_EvenIfNullOrEmpty(string propertyValue)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = propertyValue, };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act
            var result = helper.EditorFor(m => m.Property1);

            // Assert
            Assert.Equal(
                "<input class=\"HtmlEncode[[text-box single-line]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"\" />",
                HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void EditorFor_DoesNotWrapExceptionThrowsDuringViewRendering()
        {
            // Arrange
            var expectedMessage = "my exception message";
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Test string", };
            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Returns(Task.Run(() =>
                {
                    throw new FormatException(expectedMessage);
                }));
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.Found("test-view", view.Object));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act and Assert
            var ex = Assert.Throws<FormatException>(() => helper.EditorFor(m => m.Property1));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void EditorForModel_CallsFindPartialView_WithExpectedPath()
        {
            // Arrange
            var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), 
                                              It.Is<string>(view => String.Equals(view, 
                                                                                  "EditorTemplates/String"))))
                .Returns(ViewEngineResult.Found(string.Empty, new Mock<IView>().Object))
                .Verifiable();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(new object(), viewEngine: viewEngine.Object);

            // Act & Assert
            html.Editor(expression: string.Empty, templateName: null, htmlFieldName: null, additionalViewData: null);
            viewEngine.Verify();
        }

        private class OrderedModel
        {
            [Display(Order = 10001)]
            public string LastProperty { get; set; }

            public string Property3 { get; set; }
            public string Property1 { get; set; }
            public string Property2 { get; set; }

            [Display(Order = 23)]
            public string OrderedProperty3 { get; set; }
            [Display(Order = 23)]
            public string OrderedProperty2 { get; set; }
            [Display(Order = 23)]
            public string OrderedProperty1 { get; set; }
        }

        private class StubbyHtmlHelper : IHtmlHelper, ICanHasViewContext
        {
            private readonly IHtmlHelper _innerHelper;

            public StubbyHtmlHelper(IHtmlHelper innerHelper)
            {
                _innerHelper = innerHelper;
            }

            public Html5DateRenderingMode Html5DateRenderingMode
            {
                get { return _innerHelper.Html5DateRenderingMode; }
                set { _innerHelper.Html5DateRenderingMode = value; }
            }

            public string IdAttributeDotReplacement
            {
                get { return _innerHelper.IdAttributeDotReplacement; }
            }

            public IModelMetadataProvider MetadataProvider
            {
                get { return _innerHelper.MetadataProvider; }
            }

            public dynamic ViewBag
            {
                get { return _innerHelper.ViewBag; }
            }

            public ViewContext ViewContext
            {
                get { return _innerHelper.ViewContext; }
            }

            public ViewDataDictionary ViewData
            {
                get { return _innerHelper.ViewData; }
            }

            public ITempDataDictionary TempData
            {
                get { return _innerHelper.TempData; }
            }

            public UrlEncoder UrlEncoder
            {
                get { return _innerHelper.UrlEncoder; }
            }

            public JavaScriptEncoder JavaScriptEncoder
            {
                get { return _innerHelper.JavaScriptEncoder; }
            }

            public void Contextualize(ViewContext viewContext)
            {
                (_innerHelper as ICanHasViewContext)?.Contextualize(viewContext);
            }

            public IHtmlContent ActionLink(
                string linkText,
                string actionName,
                string controllerName,
                string protocol,
                string hostname,
                string fragment,
                object routeValues,
                object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent AntiForgeryToken()
            {
                throw new NotImplementedException();
            }

            public MvcForm BeginForm(
                string actionName,
                string controllerName,
                object routeValues,
                FormMethod method,
                object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public MvcForm BeginRouteForm(
                string routeName,
                object routeValues,
                FormMethod method,
                object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent CheckBox(string name, bool? isChecked, object htmlAttributes)
            {
                return HelperName("__CheckBox__", htmlAttributes);
            }

            public IHtmlContent Display(
                string expression,
                string templateName,
                string htmlFieldName,
                object additionalViewData)
            {
                throw new NotImplementedException();
            }

            public string DisplayName(string expression)
            {
                throw new NotImplementedException();
            }

            public string DisplayText(string name)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent DropDownList(
                string name,
                IEnumerable<SelectListItem> selectList,
                string optionLabel,
                object htmlAttributes)
            {
                return HelperName("__DropDownList__", htmlAttributes);
            }

            public IHtmlContent Editor(
                string expression,
                string templateName,
                string htmlFieldName,
                object additionalViewData)
            {
                return _innerHelper.Editor(expression, templateName, htmlFieldName, additionalViewData);
            }

            public string Encode(string value)
            {
                throw new NotImplementedException();
            }

            public string Encode(object value)
            {
                return _innerHelper.Encode(value);
            }

            public void EndForm()
            {
                throw new NotImplementedException();
            }

            public string FormatValue(object value, string format)
            {
                throw new NotImplementedException();
            }

            public string GenerateIdFromName(string name)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ModelClientValidationRule> GetClientValidationRules(
                ModelExplorer modelExplorer,
                string name)
            {
                return Enumerable.Empty<ModelClientValidationRule>();
            }

            public IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct
            {
                throw new NotImplementedException();
            }

            public IEnumerable<SelectListItem> GetEnumSelectList(Type enumType)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent Hidden(string name, object value, object htmlAttributes)
            {
                return HelperName("__Hidden__", htmlAttributes);
            }

            public string Id(string name)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent Label(string expression, string labelText, object htmlAttributes)
            {
                return HelperName("__Label__", htmlAttributes);
            }

            public IHtmlContent ListBox(string name, IEnumerable<SelectListItem> selectList, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public string Name(string name)
            {
                throw new NotImplementedException();
            }

            public Task<IHtmlContent> PartialAsync(
                string partialViewName,
                object model,
                ViewDataDictionary viewData)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent Password(string name, object value, object htmlAttributes)
            {
                return HelperName("__Password__", htmlAttributes);
            }

            public IHtmlContent RadioButton(string name, object value, bool? isChecked, object htmlAttributes)
            {
                return HelperName("__RadioButton__", htmlAttributes);
            }

            public IHtmlContent Raw(object value)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent Raw(string value)
            {
                throw new NotImplementedException();
            }

            public Task RenderPartialAsync(string partialViewName, object model, ViewDataDictionary viewData)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent RouteLink(
                string linkText,
                string routeName,
                string protocol,
                string hostName,
                string fragment,
                object routeValues,
                object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public IHtmlContent TextArea(string name, string value, int rows, int columns, object htmlAttributes)
            {
                return HelperName("__TextArea__", htmlAttributes);
            }

            public IHtmlContent TextBox(string name, object value, string format, object htmlAttributes)
            {
                return HelperName("__TextBox__", htmlAttributes);
            }

            public IHtmlContent ValidationMessage(string modelName, string message, object htmlAttributes, string tag)
            {
                return HelperName("__ValidationMessage__", htmlAttributes);
            }

            public IHtmlContent ValidationSummary(
                bool excludePropertyErrors,
                string message,
                object htmlAttributes,
                string tag)
            {
                throw new NotImplementedException();
            }

            public string Value(string name, string format)
            {
                throw new NotImplementedException();
            }

            private IHtmlContent HelperName(string name, object htmlAttributes)
            {
                var htmlAttributesDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
                var htmlAttributesString =
                    string.Join(" ", htmlAttributesDictionary.Select(entry => $"{ entry.Key }='{ entry.Value }'"));
                var helperName = $"{ name } { htmlAttributesString }";

                return new HtmlString(helperName.TrimEnd());
            }
        }
    }
}
#endif