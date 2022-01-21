// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class HtmlHelperTest
{
    public static TheoryData<object, KeyValuePair<string, object>> IgnoreCaseTestData
    {
        get
        {
            return new TheoryData<object, KeyValuePair<string, object>>
                {
                    {
                        new
                        {
                            selected = true,
                            SeLeCtEd = false
                        },
                        new KeyValuePair<string, object>("selected", false)
                    },
                    {
                        new
                        {
                            SeLeCtEd = false,
                            selected = true
                        },
                        new KeyValuePair<string, object>("SeLeCtEd", true)
                    },
                    {
                        new
                        {
                            SelECTeD = false,
                            SeLECTED = true
                        },
                        new KeyValuePair<string, object>("SelECTeD", true)
                    }
                };
        }
    }

    // value, expectedString
    public static TheoryData<object, string> EncodeDynamicTestData
    {
        get
        {
            var data = new TheoryData<object, string>
                {
                    { null, string.Empty },
                    // Dynamic implementation calls the string overload when possible.
                    { string.Empty, string.Empty },
                    { "<\">", "HtmlEncode[[<\">]]" },
                    { "<br />", "HtmlEncode[[<br />]]" },
                    { "<b>bold</b>", "HtmlEncode[[<b>bold</b>]]" },
                    { new ObjectWithToStringOverride(), "HtmlEncode[[<b>boldFromObject</b>]]" },
                };

            return data;
        }
    }

    // value, expectedString
    public static TheoryData<object, string> EncodeObjectTestData
    {
        get
        {
            var data = new TheoryData<object, string>
                {
                    { null, string.Empty },
                    { string.Empty, string.Empty },
                    { "<\">", "HtmlEncode[[<\">]]" },
                    { "<br />", "HtmlEncode[[<br />]]" },
                    { "<b>bold</b>", "HtmlEncode[[<b>bold</b>]]" },
                    { new ObjectWithToStringOverride(), "HtmlEncode[[<b>boldFromObject</b>]]" },
                };

            return data;
        }
    }

    // value, expectedString
    public static TheoryData<string, string> EncodeStringTestData
    {
        get
        {
            return new TheoryData<string, string>
                {
                    { null, string.Empty },
                    // String overload does not encode the empty string.
                    { string.Empty, string.Empty },
                    { "<\">", "HtmlEncode[[<\">]]" },
                    { "<br />", "HtmlEncode[[<br />]]" },
                    { "<b>bold</b>", "HtmlEncode[[<b>bold</b>]]" },
                };
        }
    }

    // value, expectedString
    public static TheoryData<object, string> RawObjectTestData
    {
        get
        {
            var data = new TheoryData<object, string>
                {
                    { new ObjectWithToStringOverride(), "<b>boldFromObject</b>" },
                };

            foreach (var item in RawStringTestData)
            {
                data.Add(item[0], (string)item[1]);
            }

            return data;
        }
    }

    // value, expectedString
    public static TheoryData<string, string> RawStringTestData
    {
        get
        {
            return new TheoryData<string, string>
                {
                    { null, string.Empty },
                    { string.Empty, string.Empty },
                    { "<\">", "<\">" },
                    { "<br />", "<br />" },
                    { "<b>bold</b>", "<b>bold</b>" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(IgnoreCaseTestData))]
    public void AnonymousObjectToHtmlAttributes_IgnoresPropertyCase(
        object htmlAttributeObject,
        KeyValuePair<string, object> expectedEntry)
    {
        // Act
        var result = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributeObject);

        // Assert
        var entry = Assert.Single(result);
        Assert.Equal(expectedEntry, entry);
    }

    [Theory]
    [MemberData(nameof(EncodeDynamicTestData))]
    public void EncodeDynamic_ReturnsExpectedString(object value, string expectedString)
    {
        // Arrange
        // Important to preserve these particular variable types. Otherwise may end up testing different runtime
        // (not compiler) behaviors.
        dynamic dynamicValue = value;
        IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
            DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.Encode(dynamicValue);

        // Assert
        Assert.Equal(expectedString, result);
    }

    [Theory]
    [MemberData(nameof(EncodeDynamicTestData))]
    public void EncodeDynamic_ReturnsExpectedString_WithBaseHelper(object value, string expectedString)
    {
        // Arrange
        // Important to preserve these particular variable types. Otherwise may end up testing different runtime
        // (not compiler) behaviors.
        dynamic dynamicValue = value;
        IHtmlHelper helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.Encode(dynamicValue);

        // Assert
        Assert.Equal(expectedString, result);
    }

    [Theory]
    [MemberData(nameof(EncodeObjectTestData))]
    public void EncodeObject_ReturnsExpectedString(object value, string expectedString)
    {
        // Arrange
        // Important to preserve this particular variable type and the (object) type of the value parameter.
        // Otherwise may end up testing different runtime (not compiler) behaviors.
        IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
            DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.Encode(value);

        // Assert
        Assert.Equal(expectedString, result);
    }

    [Theory]
    [MemberData(nameof(EncodeStringTestData))]
    public void EncodeString_ReturnsExpectedString(string value, string expectedString)
    {
        // Arrange
        // Important to preserve this particular variable type and the (string) type of the value parameter.
        // Otherwise may end up testing different runtime (not compiler) behaviors.
        IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
            DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.Encode(value);

        // Assert
        Assert.Equal(expectedString, result);
    }

    [Theory]
    [MemberData(nameof(RawObjectTestData))]
    public void RawDynamic_ReturnsExpectedString(object value, string expectedString)
    {
        // Arrange
        // Important to preserve these particular variable types. Otherwise may end up testing different runtime
        // (not compiler) behaviors.
        dynamic dynamicValue = value;
        IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
            DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.Raw(dynamicValue);

        // Assert
        Assert.Equal(expectedString, result.ToString());
    }

    [Theory]
    [MemberData(nameof(RawObjectTestData))]
    public void RawDynamic_ReturnsExpectedString_WithBaseHelper(object value, string expectedString)
    {
        // Arrange
        // Important to preserve these particular variable types. Otherwise may end up testing different runtime
        // (not compiler) behaviors.
        dynamic dynamicValue = value;
        IHtmlHelper helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.Raw(dynamicValue);

        // Assert
        Assert.Equal(expectedString, result.ToString());
    }

    [Theory]
    [MemberData(nameof(RawObjectTestData))]
    public void RawObject_ReturnsExpectedString(object value, string expectedString)
    {
        // Arrange
        // Important to preserve this particular variable type and the (object) type of the value parameter.
        // Otherwise may end up testing different runtime (not compiler) behaviors.
        IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
            DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.Raw(value);

        // Assert
        Assert.Equal(expectedString, result.ToString());
    }

    [Theory]
    [MemberData(nameof(RawStringTestData))]
    public void RawString_ReturnsExpectedString(string value, string expectedString)
    {
        // Arrange
        // Important to preserve this particular variable type and the (string) type of the value parameter.
        // Otherwise may end up testing different runtime (not compiler) behaviors.
        IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
            DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.Raw(value);

        // Assert
        Assert.Equal(expectedString, result.ToString());
    }

    [Fact]
    public void Contextualize_WorksWithCovariantViewDataDictionary()
    {
        // Arrange
        var helperToContextualize = DefaultTemplatesUtilities
            .GetHtmlHelper<BaseModel>(model: null);

        var viewContext = DefaultTemplatesUtilities
            .GetHtmlHelper<DerivedModel>(model: null)
            .ViewContext;

        // Act
        helperToContextualize.Contextualize(viewContext);

        // Assert
        Assert.IsType<ViewDataDictionary<BaseModel>>(
            helperToContextualize.ViewData);

        Assert.Same(helperToContextualize.ViewContext, viewContext);
    }

    [Fact]
    public void Contextualize_ThrowsIfViewDataDictionariesAreNotCompatible()
    {
        // Arrange
        var helperToContextualize = DefaultTemplatesUtilities
            .GetHtmlHelper<BaseModel>(model: null);

        var viewContext = DefaultTemplatesUtilities
            .GetHtmlHelper<NonDerivedModel>(model: null)
            .ViewContext;

        var expectedMessage = $"Property '{nameof(ViewContext.ViewData)}' is of type " +
            $"'{typeof(ViewDataDictionary<NonDerivedModel>).FullName}'," +
            $" but this method requires a value of type '{typeof(ViewDataDictionary<BaseModel>).FullName}'.";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>("viewContext", () => helperToContextualize.Contextualize(viewContext));
        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void Contextualize_ThrowsForNonGenericViewDataDictionaries()
    {
        // Arrange
        var helperToContextualize = DefaultTemplatesUtilities
            .GetHtmlHelper<BaseModel>(model: null);

        var viewContext = DefaultTemplatesUtilities
            .GetHtmlHelper<BaseModel>(model: null)
            .ViewContext;
        viewContext.ViewData = new ViewDataDictionary(viewContext.ViewData);

        var expectedMessage = $"Property '{nameof(ViewContext.ViewData)}' is of type " +
            $"'{typeof(ViewDataDictionary).FullName}'," +
            $" but this method requires a value of type '{typeof(ViewDataDictionary<BaseModel>).FullName}'.";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>("viewContext", () => helperToContextualize.Contextualize(viewContext));
        Assert.Contains(expectedMessage, exception.Message);
    }

    private class BaseModel
    {
        public string Name { get; set; }
    }

    private class DerivedModel : BaseModel
    {
    }

    private class NonDerivedModel
    {
    }

    [Theory]
    [InlineData("SomeName", "SomeName")]
    [InlineData("Obj1.Prop1", "Obj1_Prop1")]
    [InlineData("Obj1.Prop1[0]", "Obj1_Prop1_0_")]
    [InlineData("Obj1.Prop1[0].Prop2", "Obj1_Prop1_0__Prop2")]
    public void GenerateIdFromName_ReturnsExpectedValues(string fullname, string expected)
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.GenerateIdFromName(fullname);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(FormMethod.Get, "get")]
    [InlineData(FormMethod.Post, "post")]
    [InlineData((FormMethod)42, "post")]
    public void GetFormMethodString_ReturnsExpectedValues(FormMethod method, string expected)
    {
        // Act
        var result = HtmlHelper.GetFormMethodString(method);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("-{0}-", "-<b>boldFromObject</b>-")]
    [InlineData("-%{0}%-", "-%<b>boldFromObject</b>%-")]
    [InlineData("-=={0}=={0}==-", "-==<b>boldFromObject</b>==<b>boldFromObject</b>==-")]
    public void FormatValue_ReturnsExpectedValues(string format, string expected)
    {
        // Arrange
        var value = new ObjectWithToStringOverride();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.FormatValue(value, format);

        // Assert
        Assert.Equal(expected, result);
    }

    private class ObjectWithToStringOverride
    {
        public override string ToString()
        {
            return "<b>boldFromObject</b>";
        }
    }
}
