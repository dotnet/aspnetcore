// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class HtmlHelperTextAreaTest
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

    public static TheoryData TextAreaFor_UsesModelValueForComplexExpressionsData
    {
        get
        {
            return new TheoryData<Expression<Func<ComplexModel, string>>, string>
                {
                    {
                        model => model.Property3["key"],
                        "<textarea id=\"HtmlEncode[[pre_Property3_key_]]\" name=\"HtmlEncode[[pre.Property3[key]]]\">" + Environment.NewLine +
                        "HtmlEncode[[Prop3Val]]</textarea>"
                    },
                    {
                        model => model.Property4.Property5,
                        "<textarea id=\"HtmlEncode[[pre_Property4_Property5]]\" name=\"HtmlEncode[[pre.Property4.Property5]]\">" + Environment.NewLine +
                        "HtmlEncode[[Prop5Val]]</textarea>"
                    },
                    {
                        model => model.Property4.Property6[0],
                        "<textarea id=\"HtmlEncode[[pre_Property4_Property6_0_]]\" name=\"HtmlEncode[[pre.Property4.Property6[0]]]\">" + Environment.NewLine +
                        "HtmlEncode[[Prop6Val]]</textarea>"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(TextAreaFor_UsesModelValueForComplexExpressionsData))]
    public void TextAreaFor_ComplexExpressions_UsesModelValueForComplexExpressions(
        Expression<Func<ComplexModel, string>> expression,
        string expected)
    {
        // Arrange
        var model = new ComplexModel();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "pre";

        helper.ViewData.Model.Property3["key"] = "Prop3Val";
        helper.ViewData.Model.Property4.Property5 = "Prop5Val";
        helper.ViewData.Model.Property4.Property6.Add("Prop6Val");

        // Act
        var result = helper.TextAreaFor(expression);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    public static TheoryData TextAreaFor_UsesModelStateValueForComplexExpressionsData
    {
        get
        {
            return new TheoryData<Expression<Func<ComplexModel, string>>, string>
                {
                    {
                        model => model.Property3["key"],
                        "<textarea id=\"HtmlEncode[[pre_Property3_key_]]\" name=\"HtmlEncode[[pre.Property3[key]]]\">" + Environment.NewLine +
                        "HtmlEncode[[MProp3Val]]</textarea>"
                    },
                    {
                        model => model.Property4.Property5,
                        "<textarea id=\"HtmlEncode[[pre_Property4_Property5]]\" name=\"HtmlEncode[[pre.Property4.Property5]]\">" + Environment.NewLine +
                        "HtmlEncode[[MProp5Val]]</textarea>"
                    },
                    {
                        model => model.Property4.Property6[0],
                        "<textarea id=\"HtmlEncode[[pre_Property4_Property6_0_]]\" name=\"HtmlEncode[[pre.Property4.Property6[0]]]\">" + Environment.NewLine +
                        "HtmlEncode[[MProp6Val]]</textarea>"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(TextAreaFor_UsesModelStateValueForComplexExpressionsData))]
    public void TextAreaFor_ComplexExpressions_UsesModelStateValueForComplexExpressions(
        Expression<Func<ComplexModel, string>> expression,
        string expected)
    {
        // Arrange
        var model = new ComplexModel();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "pre";

        helper.ViewData.ModelState.SetModelValue("pre.Property3[key]", "MProp3Val", "MProp3Val");
        helper.ViewData.ModelState.SetModelValue("pre.Property4.Property5", "MProp5Val", "MProp5Val");
        helper.ViewData.ModelState.SetModelValue("pre.Property4.Property6[0]", "MProp6Val", "MProp6Val");

        helper.ViewData.Model.Property3["key"] = "Prop3Val";
        helper.ViewData.Model.Property4.Property5 = "Prop5Val";
        helper.ViewData.Model.Property4.Property6.Add("Prop6Val");

        // Act
        var result = helper.TextAreaFor(expression);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    public class ComplexModel
    {
        public string Property1 { get; set; }

        public byte[] Bytes { get; set; }

        [Required]
        public string Property2 { get; set; }

        public Dictionary<string, string> Property3 { get; } = new Dictionary<string, string>();

        public NestedClass Property4 { get; } = new NestedClass();
    }

    public class NestedClass
    {
        public string Property5 { get; set; }

        public List<string> Property6 { get; } = new List<string>();
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
