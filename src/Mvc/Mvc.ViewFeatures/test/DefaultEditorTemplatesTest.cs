// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

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
                    { "DateTime", "__TextBox__ class='text-box single-line' type='datetime-local'" },
                    { "datetime", "__TextBox__ class='text-box single-line' type='datetime-local'" },
                    { "DateTime-local", "__TextBox__ class='text-box single-line' type='datetime-local'" },
                    { "DATETIME-LOCAL", "__TextBox__ class='text-box single-line' type='datetime-local'" },
                    { "datetimeoffset", "__TextBox__ class='text-box single-line' type='text'" },
                    { "DateTimeOffset", "__TextBox__ class='text-box single-line' type='text'" },
                    { "Time", "__TextBox__ class='text-box single-line' type='time'" },
                    { "time", "__TextBox__ class='text-box single-line' type='time'" },
                    { "Month", "__TextBox__ class='text-box single-line' type='month'" },
                    { "month", "__TextBox__ class='text-box single-line' type='month'" },
                    { "Week", "__TextBox__ class='text-box single-line' type='week'" },
                    { "week", "__TextBox__ class='text-box single-line' type='week'" },
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

    // label's IHtmlContent -> expected label text
    public static TheoryData<IHtmlContent, string> ObjectTemplate_ChecksWriteTo_NotToStringData
    {
        get
        {
            // Similar to HtmlString.Empty today.
            var noopContentWithEmptyToString = new Mock<IHtmlContent>(MockBehavior.Strict);
            noopContentWithEmptyToString
                .Setup(c => c.ToString())
                .Returns(string.Empty);
            noopContentWithEmptyToString.Setup(c => c.WriteTo(It.IsAny<TextWriter>(), It.IsAny<HtmlEncoder>()));

            // Similar to an empty StringHtmlContent today.
            var noopContentWithNonEmptyToString = new Mock<IHtmlContent>(MockBehavior.Strict);
            noopContentWithNonEmptyToString
                .Setup(c => c.ToString())
                .Returns(typeof(StringHtmlContent).FullName);
            noopContentWithNonEmptyToString.Setup(c => c.WriteTo(It.IsAny<TextWriter>(), It.IsAny<HtmlEncoder>()));

            // Makes noop calls on the TextWriter.
            var busyNoopContentWithNonEmptyToString = new Mock<IHtmlContent>(MockBehavior.Strict);
            busyNoopContentWithNonEmptyToString
                .Setup(c => c.ToString())
                .Returns(typeof(StringHtmlContent).FullName);
            busyNoopContentWithNonEmptyToString
                .Setup(c => c.WriteTo(It.IsAny<TextWriter>(), It.IsAny<HtmlEncoder>()))
                .Callback<TextWriter, HtmlEncoder>((writer, encoder) =>
                {
                    writer.Write(string.Empty);
                    writer.Write(new char[0]);
                    writer.Write((char[])null);
                    writer.Write((object)null);
                    writer.Write((string)null);
                    writer.Write(format: "{0}", arg0: null);
                    writer.Write(new char[] { 'a', 'b', 'c' }, index: 1, count: 0);
                });

            // Unrealistic but covers all the bases.
            var writingContentWithEmptyToString = new Mock<IHtmlContent>(MockBehavior.Strict);
            writingContentWithEmptyToString
                .Setup(c => c.ToString())
                .Returns(string.Empty);
            writingContentWithEmptyToString
                .Setup(c => c.WriteTo(It.IsAny<TextWriter>(), It.IsAny<HtmlEncoder>()))
                .Callback<TextWriter, HtmlEncoder>((writer, encoder) => writer.Write("Some string"));

            // Similar to TagBuilder today.
            var writingContentWithNonEmptyToString = new Mock<IHtmlContent>(MockBehavior.Strict);
            writingContentWithNonEmptyToString
                .Setup(c => c.ToString())
                .Returns(typeof(TagBuilder).FullName);
            writingContentWithNonEmptyToString
                .Setup(c => c.WriteTo(It.IsAny<TextWriter>(), It.IsAny<HtmlEncoder>()))
                .Callback<TextWriter, HtmlEncoder>((writer, encoder) => writer.Write("Some string"));

            // label's IHtmlContent -> expected label text
            return new TheoryData<IHtmlContent, string>
                {
                    // Types HtmlHelper actually uses.
                    { HtmlString.Empty, string.Empty },
                    {
                        new TagBuilder("label"),
                        "<div class=\"HtmlEncode[[editor-label]]\"><label></label></div>" + Environment.NewLine
                    },

                    // Another IHtmlContent implementation that does not override ToString().
                    { new StringHtmlContent(string.Empty), string.Empty },

                    // Mocks
                    { noopContentWithEmptyToString.Object, string.Empty },
                    { noopContentWithNonEmptyToString.Object, string.Empty },
                    { busyNoopContentWithNonEmptyToString.Object, string.Empty },
                    {
                        writingContentWithEmptyToString.Object,
                        "<div class=\"HtmlEncode[[editor-label]]\">Some string</div>" + Environment.NewLine
                    },
                    {
                        writingContentWithNonEmptyToString.Object,
                        "<div class=\"HtmlEncode[[editor-label]]\">Some string</div>" + Environment.NewLine
                    },
                };
        }
    }

    [Fact]
    public void ObjectTemplateEditsSimplePropertiesOnObjectByDefault()
    {
        // Arrange
        var expected =
            "<div class=\"HtmlEncode[[editor-label]]\"><label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Property1]]</label></div>" +
            Environment.NewLine +
            "<div class=\"HtmlEncode[[editor-field]]\">Model = p1, ModelType = System.String, PropertyName = Property1, SimpleDisplayText = p1 " +
            "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[true]]\">" +
            "</span></div>" +
            Environment.NewLine +
            "<div class=\"HtmlEncode[[editor-label]]\"><label for=\"HtmlEncode[[Property2]]\">HtmlEncode[[Prop2]]</label></div>" +
            Environment.NewLine +
            "<div class=\"HtmlEncode[[editor-field]]\">Model = (null), ModelType = System.String, PropertyName = Property2, SimpleDisplayText = (null) " +
            "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property2]]\" data-valmsg-replace=\"HtmlEncode[[true]]\">" +
            "</span></div>" +
            Environment.NewLine;

        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = DefaultEditorTemplates.ObjectTemplate(html);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    // Prior to aspnet/Mvc#6638 fix, helper did not generate Property1 <label> or containing <div> with this setup.
    // Expect almost the same HTML as in ObjectTemplateEditsSimplePropertiesOnObjectByDefault(). Only difference is
    // the <div class="editor-label">...</div> is not present for Property1.
    [Fact]
    public void ObjectTemplateSkipsLabel_IfDisplayNameIsEmpty()
    {
        // Arrange
        var expected =
            "<div class=\"HtmlEncode[[editor-label]]\"><label for=\"HtmlEncode[[Property1]]\"></label></div>" +
            Environment.NewLine +
            "<div class=\"HtmlEncode[[editor-field]]\">Model = p1, ModelType = System.String, PropertyName = Property1, SimpleDisplayText = p1 " +
            "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[true]]\">" +
            "</span></div>" +
            Environment.NewLine +
            "<div class=\"HtmlEncode[[editor-label]]\"><label for=\"HtmlEncode[[Property2]]\">HtmlEncode[[Prop2]]</label></div>" +
            Environment.NewLine +
            "<div class=\"HtmlEncode[[editor-field]]\">Model = (null), ModelType = System.String, PropertyName = Property2, SimpleDisplayText = (null) " +
            "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property2]]\" data-valmsg-replace=\"HtmlEncode[[true]]\">" +
            "</span></div>" +
            Environment.NewLine;

        var provider = new TestModelMetadataProvider();
        provider
            .ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>(
                nameof(DefaultTemplatesUtilities.ObjectTemplateModel.Property1))
            .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model, provider);

        // Act
        var result = DefaultEditorTemplates.ObjectTemplate(html);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Theory]
    [MemberData(nameof(ObjectTemplate_ChecksWriteTo_NotToStringData))]
    public void ObjectTemplate_ChecksWriteTo_NotToString(IHtmlContent labelContent, string expectedLabel)
    {
        // Arrange
        var expected =
            expectedLabel +
            "<div class=\"HtmlEncode[[editor-field]]\">Model = (null), ModelType = System.String, PropertyName = Property1, SimpleDisplayText = (null) " +
            "</div>" +
            Environment.NewLine +
            expectedLabel +
            "<div class=\"HtmlEncode[[editor-field]]\">Model = (null), ModelType = System.String, PropertyName = Property2, SimpleDisplayText = (null) " +
            "</div>" +
            Environment.NewLine;

        var helperToCopy = DefaultTemplatesUtilities.GetHtmlHelper();
        var helperMock = new Mock<IHtmlHelper>(MockBehavior.Strict);
        helperMock.SetupGet(h => h.ViewContext).Returns(helperToCopy.ViewContext);
        helperMock.SetupGet(h => h.ViewData).Returns(helperToCopy.ViewData);
        helperMock
            .Setup(h => h.Label(
                It.Is<string>(s => string.Equals("Property1", s, StringComparison.Ordinal) ||
                    string.Equals("Property2", s, StringComparison.Ordinal)),
                null,   // labelText
                null))  // htmlAttributes
            .Returns(labelContent);
        helperMock
            .Setup(h => h.ValidationMessage(
                It.Is<string>(s => string.Equals("Property1", s, StringComparison.Ordinal) ||
                    string.Equals("Property2", s, StringComparison.Ordinal)),
                null,   // message
                null,   // htmlAttributes
                null))  // tag
            .Returns(HtmlString.Empty);

        // Act
        var result = DefaultEditorTemplates.ObjectTemplate(helperMock.Object);

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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var result = DefaultEditorTemplates.ObjectTemplate(htmlHelper);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ObjectTemplate_HonorsHideSurroundingHtml()
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
    public void HiddenInputTemplate_HonorsHideSurroundingHtml()
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

    [Fact]
    public void PasswordTemplate_ReturnsInputElement_IgnoresValues()
    {
        // Arrange
        var expected = "<input class=\"HtmlEncode[[text-box single-line password]]\" " +
            "id=\"HtmlEncode[[FieldPrefix]]\" name=\"HtmlEncode[[FieldPrefix]]\" " +
            "type=\"HtmlEncode[[password]]\" />";

        // Template ignores Model.
        var model = "Model string";

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        var viewData = helper.ViewData;
        var templateInfo = viewData.TemplateInfo;
        templateInfo.HtmlFieldPrefix = "FieldPrefix";

        // Template ignores FormattedModelValue, ModelState and ViewData.
        templateInfo.FormattedModelValue = "Formatted string";
        viewData.ModelState.SetModelValue("FieldPrefix", "Raw model string", "Attempted model string");
        viewData["FieldPrefix"] = "ViewData string";

        // Act
        var result = DefaultEditorTemplates.PasswordTemplate(helper);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordTemplate_ReturnsInputElement_UsesHtmlAttributes()
    {
        // Arrange
        var expected = "<input class=\"HtmlEncode[[super text-box single-line password]]\" " +
            "id=\"HtmlEncode[[FieldPrefix]]\" name=\"HtmlEncode[[FieldPrefix]]\" " +
            "type=\"HtmlEncode[[password]]\" value=\"HtmlEncode[[Html attributes string]]\" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper<string>(model: null);
        var viewData = helper.ViewData;
        var templateInfo = viewData.TemplateInfo;
        templateInfo.HtmlFieldPrefix = "FieldPrefix";

        viewData["htmlAttributes"] = new { @class = "super", value = "Html attributes string" };

        // Act
        var result = DefaultEditorTemplates.PasswordTemplate(helper);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Theory]
    [MemberData(nameof(TemplateNameData))]
    public void Editor_CallsExpectedHtmlHelper(string templateName, string expectedResult)
    {
        // Arrange
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "True" };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

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
            localizerFactory: null,
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

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
            localizerFactory: null,
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

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
            localizerFactory: null,
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

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
            localizerFactory: null,
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        helper.ViewData["Property1"] = "ViewData string";

        // Act
        var result = helper.Editor("Property1");

        // Assert
        Assert.Equal(
            "<input class=\"HtmlEncode[[text-box single-line]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[ViewData string]]\" />",
            HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void Editor_InputTypeDateTime_RendersAsDateTime()
    {
        // Arrange
        var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("DateTimeOffset");
        var expectedInput = "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
            $"data-val-required=\"HtmlEncode[[{requiredMessage}]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
            "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[datetime]]\" value=\"HtmlEncode[[2000-01-02T03:04:05.060+00:00]]\" />";

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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

        var provider = new TestModelMetadataProvider();

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(
            model,
            Mock.Of<IUrlHelper>(),
            viewEngine.Object,
            provider);
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

        // Act
        var result = helper.Editor(
            string.Empty,
            new { htmlAttributes = new { type = "datetime" } });

        // Assert
        Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
    }

    // Html5DateRenderingMode.Rfc3339 is enabled by default.
    [Theory]
    [InlineData(null, null, "2000-01-02T03:04:05.060-05:00", "text")]
    [InlineData("date", null, "2000-01-02", "date")]
    [InlineData("date", "{0:d}", "02/01/2000", "date")]
    [InlineData("datetime", null, "2000-01-02T03:04:05.060", "datetime-local")]
    [InlineData("datetime-local", null, "2000-01-02T03:04:05.060", "datetime-local")]
    [InlineData("DateTimeOffset", null, "2000-01-02T03:04:05.060-05:00", "text")]
    [InlineData("DateTimeOffset", "{0:o}", "2000-01-02T03:04:05.0600000-05:00", "text")]
    [InlineData("time", null, "03:04:05.060", "time")]
    [InlineData("time", "{0:t}", "03:04", "time")]
    [InlineData("month", null, "2000-01", "month")]
    [InlineData("month", "{0:yyyy-MM}", "2000-01", "month")]
    [InlineData("week", null, "1999-W52", "week")]
    [InlineData("week", "{0:yyyy-'W1'}", "2000-W1", "week")]
    [ReplaceCulture]
    public void Editor_FindsCorrectDateOrTimeTemplate_WithTimeOffset(
        string dataTypeName,
        string editFormatString,
        string expectedValue,
        string expectedType)
    {
        // Arrange
        var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("DateTimeOffset");
        var expectedInput = "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
            $"data-val-required=\"HtmlEncode[[{requiredMessage}]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
            "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[" +
            expectedType +
            "]]\" value=\"HtmlEncode[[" + expectedValue + "]]\" />";

        var offset = TimeSpan.FromHours(-5);
        var model = new DateTimeOffset(
            year: 2000,
            month: 1,
            day: 2,
            hour: 3,
            minute: 4,
            second: 5,
            millisecond: 60,
            offset: offset);
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

        var provider = new TestModelMetadataProvider();
        provider.ForType<DateTimeOffset>().DisplayDetails(dd =>
        {
            dd.DataTypeName = dataTypeName;
            dd.EditFormatString = editFormatString; // What [DataType] does for given type.
            dd.HasNonDefaultEditFormat = true;
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(
            model,
            Mock.Of<IUrlHelper>(),
            viewEngine.Object,
            provider);
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

        // Act
        var result = helper.Editor(string.Empty);

        // Assert
        Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
    }

    // Html5DateRenderingMode.Rfc3339 can be disabled.
    [Theory]
    [InlineData(null, null, "02/01/2000 03:04:05 -05:00", "text")]
    [InlineData("date", null, "02/01/2000 03:04:05 -05:00", "date")]
    [InlineData("date", "{0:d}", "02/01/2000", "date")]
    [InlineData("datetime", null, "02/01/2000 03:04:05 -05:00", "datetime-local")]
    [InlineData("datetime-local", null, "02/01/2000 03:04:05 -05:00", "datetime-local")]
    [InlineData("DateTimeOffset", null, "02/01/2000 03:04:05 -05:00", "text")]
    [InlineData("DateTimeOffset", "{0:o}", "2000-01-02T03:04:05.0600000-05:00", "text")]
    [InlineData("time", null, "02/01/2000 03:04:05 -05:00", "time")]
    [InlineData("time", "{0:t}", "03:04", "time")]
    [InlineData("month", null, "2000-01", "month")]
    [InlineData("month", "{0:yyyy-MM}", "2000-01", "month")]
    [InlineData("week", null, "1999-W52", "week")]
    [InlineData("week", "{0:yyyy-'W1'}", "2000-W1", "week")]
    [ReplaceCulture]
    public void Editor_FindsCorrectDateOrTimeTemplate_WithTimeOffset_NotRfc3339(
        string dataTypeName,
        string editFormatString,
        string expectedValue,
        string expectedType)
    {
        // Arrange
        var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("DateTimeOffset");
        var expectedInput =
            "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
            $"data-val-required=\"HtmlEncode[[{requiredMessage}]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
            "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[" +
            expectedType +
            "]]\" value=\"HtmlEncode[[" + expectedValue + "]]\" />";

        var offset = TimeSpan.FromHours(-5);
        var model = new DateTimeOffset(
            year: 2000,
            month: 1,
            day: 2,
            hour: 3,
            minute: 4,
            second: 5,
            millisecond: 60,
            offset: offset);
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

        var provider = new TestModelMetadataProvider();
        provider.ForType<DateTimeOffset>().DisplayDetails(dd =>
        {
            dd.DataTypeName = dataTypeName;
            dd.EditFormatString = editFormatString; // What [DataType] does for given type.
            dd.HasNonDefaultEditFormat = true;
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(
            model,
            Mock.Of<IUrlHelper>(),
            viewEngine.Object,
            provider);
        helper.Html5DateRenderingMode = Html5DateRenderingMode.CurrentCulture;
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

        // Act
        var result = helper.Editor(string.Empty);

        // Assert
        Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
    }

    // Html5DateRenderingMode.Rfc3339 is enabled by default.
    [Theory]
    [InlineData(null, null, "2000-01-02T03:04:05.060", "datetime-local")]
    [InlineData("date", null, "2000-01-02", "date")]
    [InlineData("date", "{0:d}", "02/01/2000", "date")]
    [InlineData("datetime", null, "2000-01-02T03:04:05.060", "datetime-local")]
    [InlineData("datetime-local", null, "2000-01-02T03:04:05.060", "datetime-local")]
    [InlineData("DateTimeOffset", null, "2000-01-02T03:04:05.060Z", "text")]
    [InlineData("DateTimeOffset", "{0:o}", "2000-01-02T03:04:05.0600000Z", "text")]
    [InlineData("time", null, "03:04:05.060", "time")]
    [InlineData("time", "{0:t}", "03:04", "time")]
    [InlineData("month", null, "2000-01", "month")]
    [InlineData("month", "{0:yyyy/MM}", "2000/01", "month")]
    [InlineData("week", null, "1999-W52", "week")]
    [InlineData("Week", "{0:yyyy/'W1'}", "2000/W1", "week")]
    [ReplaceCulture]
    public void Editor_FindsCorrectDateOrTimeTemplate_ForDateTime(
        string dataTypeName,
        string editFormatString,
        string expectedValue,
        string expectedType)
    {
        // Arrange
        var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("DateTime");
        var expectedInput = "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
            $"data-val-required=\"HtmlEncode[[{requiredMessage}]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
            "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[" +
            expectedType +
            "]]\" value=\"HtmlEncode[[" + expectedValue + "]]\" />";

        var model = new DateTime(
            year: 2000,
            month: 1,
            day: 2,
            hour: 3,
            minute: 4,
            second: 5,
            millisecond: 60,
            kind: DateTimeKind.Utc);
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

        var provider = new TestModelMetadataProvider();
        provider.ForType<DateTime>().DisplayDetails(dd =>
        {
            dd.DataTypeName = dataTypeName;
            dd.EditFormatString = editFormatString; // What [DataType] does for given type.
            dd.HasNonDefaultEditFormat = true;
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(
            model,
            Mock.Of<IUrlHelper>(),
            viewEngine.Object,
            provider);
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

        // Act
        var result = helper.Editor(string.Empty);

        // Assert
        Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
    }

    // Html5DateRenderingMode.Rfc3339 can be disabled.
    [Theory]
    [InlineData(null, null, "02/01/2000 03:04:05", "datetime-local")]
    [InlineData("date", null, "02/01/2000 03:04:05", "date")]
    [InlineData("date", "{0:d}", "02/01/2000", "date")]
    [InlineData("datetime", null, "02/01/2000 03:04:05", "datetime-local")]
    [InlineData("datetime-local", null, "02/01/2000 03:04:05", "datetime-local")]
    [InlineData("DateTimeOffset", null, "02/01/2000 03:04:05", "text")]
    [InlineData("DateTimeOffset", "{0:o}", "2000-01-02T03:04:05.0600000Z", "text")]
    [InlineData("time", "{0:t}", "03:04", "time")]
    [InlineData("month", null, "2000-01", "month")]
    [InlineData("month", "{0:yyyy/MM}", "2000/01", "month")]
    [InlineData("week", null, "1999-W52", "week")]
    [InlineData("Week", "{0:yyyy/'W1'}", "2000/W1", "week")]
    [ReplaceCulture]
    public void Editor_FindsCorrectDateOrTimeTemplate_ForDateTimeNotRfc3339(
        string dataTypeName,
        string editFormatString,
        string expectedValue,
        string expectedType)
    {
        // Arrange
        var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("DateTime");
        var expectedInput =
            "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
            $"data-val-required=\"HtmlEncode[[{requiredMessage}]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
            "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[" +
            expectedType +
            "]]\" value=\"HtmlEncode[[" + expectedValue + "]]\" />";

        var model = new DateTime(
            year: 2000,
            month: 1,
            day: 2,
            hour: 3,
            minute: 4,
            second: 5,
            millisecond: 60,
            kind: DateTimeKind.Utc);
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

        var provider = new TestModelMetadataProvider();
        provider.ForType<DateTime>().DisplayDetails(dd =>
        {
            dd.DataTypeName = dataTypeName;
            dd.EditFormatString = editFormatString; // What [DataType] does for given type.
            dd.HasNonDefaultEditFormat = true;
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(
            model,
            Mock.Of<IUrlHelper>(),
            viewEngine.Object,
            provider);
        helper.Html5DateRenderingMode = Html5DateRenderingMode.CurrentCulture;
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

        // Act
        var result = helper.Editor(string.Empty);

        // Assert
        Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Theory]
    [InlineData(null, Html5DateRenderingMode.CurrentCulture, "text")]
    [InlineData(null, Html5DateRenderingMode.Rfc3339, "text")]
    [InlineData("date", Html5DateRenderingMode.CurrentCulture, "date")]
    [InlineData("date", Html5DateRenderingMode.Rfc3339, "date")]
    [InlineData("datetime", Html5DateRenderingMode.CurrentCulture, "datetime-local")]
    [InlineData("datetime", Html5DateRenderingMode.Rfc3339, "datetime-local")]
    [InlineData("datetime-local", Html5DateRenderingMode.CurrentCulture, "datetime-local")]
    [InlineData("datetime-local", Html5DateRenderingMode.Rfc3339, "datetime-local")]
    [InlineData("time", Html5DateRenderingMode.CurrentCulture, "time")]
    [InlineData("time", Html5DateRenderingMode.Rfc3339, "time")]
    [InlineData("month", Html5DateRenderingMode.CurrentCulture, "month")]
    [InlineData("month", Html5DateRenderingMode.Rfc3339, "month")]
    [InlineData("week", Html5DateRenderingMode.CurrentCulture, "week")]
    [InlineData("week", Html5DateRenderingMode.Rfc3339, "week")]
    public void Editor_AppliesNonDefaultEditFormat(string dataTypeName, Html5DateRenderingMode renderingMode, string expectedType)
    {
        // Arrange
        var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("DateTimeOffset");
        var expectedInput = "<input class=\"HtmlEncode[[text-box single-line]]\" data-val=\"HtmlEncode[[true]]\" " +
            $"data-val-required=\"HtmlEncode[[{requiredMessage}]]\" id=\"HtmlEncode[[FieldPrefix]]\" " +
            "name=\"HtmlEncode[[FieldPrefix]]\" type=\"HtmlEncode[[" +
            expectedType +
            "]]\" value=\"HtmlEncode[[Formatted as 2000-01-02T03:04:05.0600000+00:00]]\" />";

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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));

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
        var result = helper.Editor(string.Empty);

        // Assert
        Assert.Equal(expectedInput, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void EditorFor_FindsModel()
    {
        // Arrange
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
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
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("test-view", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        helper.ViewData["Property1"] = "ViewData string";

        // Act and Assert
        var ex = Assert.Throws<FormatException>(() => helper.EditorFor(m => m.Property1));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void EditorForModel_CallsFindView_WithExpectedPath()
    {
        // Arrange
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "EditorTemplates/String", /*isMainPage*/ false))
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

    private class StubbyHtmlHelper : IHtmlHelper, IViewContextAware
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

        public void Contextualize(ViewContext viewContext)
        {
            (_innerHelper as IViewContextAware)?.Contextualize(viewContext);
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
            bool? antiforgery,
            object htmlAttributes)
        {
            throw new NotImplementedException();
        }

        public MvcForm BeginRouteForm(
            string routeName,
            object routeValues,
            FormMethod method,
            bool? antiforgery,
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
