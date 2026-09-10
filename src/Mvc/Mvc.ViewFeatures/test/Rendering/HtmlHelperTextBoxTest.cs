// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.Rendering;

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

    public static TheoryData<Expression<Func<ComplexModel, string>>, string> TextBoxFor_UsesModelValueForComplexExpressionsData
    {
        get
        {
            return new TheoryData<Expression<Func<ComplexModel, string>>, string>
                {
                    {
                        model => model.Property3["key"],
                        @"<input id=""HtmlEncode[[pre_Property3_key_]]"" name=""HtmlEncode[[pre.Property3[key]]]"" " +
                        @"type=""HtmlEncode[[text]]"" value=""HtmlEncode[[Prop3Val]]"" />"
                    },
                    {
                        model => model.Property4.Property5,
                        @"<input id=""HtmlEncode[[pre_Property4_Property5]]"" name=""HtmlEncode[[pre.Property4.Property5]]"" " +
                        @"type=""HtmlEncode[[text]]"" value=""HtmlEncode[[Prop5Val]]"" />"
                    },
                    {
                        model => model.Property4.Property6[0],
                        @"<input id=""HtmlEncode[[pre_Property4_Property6_0_]]"" " +
                        @"name=""HtmlEncode[[pre.Property4.Property6[0]]]"" type=""HtmlEncode[[text]]"" value=""HtmlEncode[[Prop6Val]]"" />"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(TextBoxFor_UsesModelValueForComplexExpressionsData))]
    public void TextBoxFor_ComplexExpressions_UsesModelValueForComplexExpressions(
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
        var result = helper.TextBoxFor(expression);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    public static TheoryData<Expression<Func<ComplexModel, string>>, string> TextBoxFor_UsesModelStateValueForComplexExpressionsData
    {
        get
        {
            return new TheoryData<Expression<Func<ComplexModel, string>>, string>
                {
                    {
                        model => model.Property3["key"],
                        @"<input id=""HtmlEncode[[pre_Property3_key_]]"" name=""HtmlEncode[[pre.Property3[key]]]"" " +
                        @"type=""HtmlEncode[[text]]"" value=""HtmlEncode[[MProp3Val]]"" />"
                    },
                    {
                        model => model.Property4.Property5,
                        @"<input id=""HtmlEncode[[pre_Property4_Property5]]"" name=""HtmlEncode[[pre.Property4.Property5]]"" " +
                        @"type=""HtmlEncode[[text]]"" value=""HtmlEncode[[MProp5Val]]"" />"
                    },
                    {
                        model => model.Property4.Property6[0],
                        @"<input id=""HtmlEncode[[pre_Property4_Property6_0_]]"" " +
                        @"name=""HtmlEncode[[pre.Property4.Property6[0]]]"" type=""HtmlEncode[[text]]"" value=""HtmlEncode[[MProp6Val]]"" />"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(TextBoxFor_UsesModelStateValueForComplexExpressionsData))]
    public void TextBoxFor_ComplexExpressions_UsesModelStateValueForComplexExpressions(
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
        var result = helper.TextBoxFor(expression);

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

    private class TextBoxModel
    {
        [Display(Prompt = "placeholder")]
        public string Property1 { get; set; }
    }
}
