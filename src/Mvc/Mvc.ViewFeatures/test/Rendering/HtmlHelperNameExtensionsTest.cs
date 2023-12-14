// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Core;

/// <summary>
/// Test the <see cref="HtmlHelperNameExtensions" /> class.
/// </summary>
public class HtmlHelperNameExtensionsTest
{
    private static readonly List<string> _staticCollection = new List<string>();
    private static readonly int _staticIndex = 6;

    private readonly List<string> _collection = new List<string>();
    private readonly int _index = 7;
    private readonly List<string[]> _nestedCollection = new List<string[]>();
    private readonly string _string = string.Empty;

    private static List<string> StaticCollection { get; }

    private static int StaticIndex { get; } = 8;

    private List<string> Collection { get; }

    private int Index { get; } = 9;

    private List<string[]> NestedCollection { get; }

    private string StringProperty { get; }

    [Fact]
    public void IdAndNameHelpers_ReturnEmptyForModel()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var idResult = helper.Id(expression: string.Empty);
        var idNullResult = helper.Id(expression: null);   // null is another alias for current model
        var idForResult = helper.IdFor(m => m);
        var idForModelResult = helper.IdForModel();
        var nameResult = helper.Name(expression: string.Empty);
        var nameNullResult = helper.Name(expression: null);
        var nameForResult = helper.NameFor(m => m);
        var nameForModelResult = helper.NameForModel();

        // Assert
        Assert.Empty(idResult);
        Assert.Empty(idNullResult);
        Assert.Empty(idForResult);
        Assert.Empty(idForModelResult);
        Assert.Empty(nameResult);
        Assert.Empty(nameNullResult);
        Assert.Empty(nameForResult);
        Assert.Empty(nameForModelResult);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("A", "A")]
    [InlineData("A[23]", "A_23_")]
    [InlineData("A[0].B", "A_0__B")]
    [InlineData("A.B.C.D", "A_B_C_D")]
    public void IdAndNameHelpers_ReturnPrefixForModel(string prefix, string expectedId)
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

        // Act
        var idResult = helper.Id(expression: string.Empty);
        var idForResult = helper.IdFor(m => m);
        var idForModelResult = helper.IdForModel();
        var nameResult = helper.Name(expression: string.Empty);
        var nameForResult = helper.NameFor(m => m);
        var nameForModelResult = helper.NameForModel();

        // Assert
        Assert.Equal(expectedId, idResult);
        Assert.Equal(expectedId, idForResult);
        Assert.Equal(expectedId, idForModelResult);
        Assert.Equal(prefix, nameResult);
        Assert.Equal(prefix, nameForResult);
        Assert.Equal(prefix, nameForModelResult);
    }

    [Fact]
    public void IdAndNameHelpers_ReturnPropertyName()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var idResult = helper.Id("Property1");
        var idForResult = helper.IdFor(m => m.Property1);
        var nameResult = helper.Name("Property1");
        var nameForResult = helper.NameFor(m => m.Property1);

        // Assert
        Assert.Equal("Property1", idResult);
        Assert.Equal("Property1", idForResult);
        Assert.Equal("Property1", nameResult);
        Assert.Equal("Property1", nameForResult);
    }

    [Theory]
    [InlineData(null, "Property1", "Property1")]
    [InlineData("", "Property1", "Property1")]
    [InlineData("A", "A.Property1", "A_Property1")]
    [InlineData("A[23]", "A[23].Property1", "A_23__Property1")]
    [InlineData("A[0].B", "A[0].B.Property1", "A_0__B_Property1")]
    [InlineData("A.B.C.D", "A.B.C.D.Property1", "A_B_C_D_Property1")]
    public void IdAndNameHelpers_ReturnPrefixAndPropertyName(string prefix, string expectedName, string expectedId)
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

        // Act
        var idResult = helper.Id("Property1");
        var idForResult = helper.IdFor(m => m.Property1);
        var nameResult = helper.Name("Property1");
        var nameForResult = helper.NameFor(m => m.Property1);

        // Assert
        Assert.Equal(expectedId, idResult);
        Assert.Equal(expectedId, idForResult);
        Assert.Equal(expectedName, nameResult);
        Assert.Equal(expectedName, nameForResult);
    }

    [Fact]
    public void IdAndNameHelpers_ReturnPropertyPath_ForNestedProperty()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OuterClass>(model: null);

        // Act
        var idResult = helper.Id("Inner.Id");
        var idForResult = helper.IdFor(m => m.Inner.Id);
        var nameResult = helper.Name("Inner.Id");
        var nameForResult = helper.NameFor(m => m.Inner.Id);

        // Assert
        Assert.Equal("Inner_Id", idResult);
        Assert.Equal("Inner_Id", idForResult);
        Assert.Equal("Inner.Id", nameResult);
        Assert.Equal("Inner.Id", nameForResult);
    }

    [Fact]
    public void IdAndNameHelpers_DoNotConsultMetadataOrMetadataProvider()
    {
        // Arrange
        var provider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
        var metadata = new Mock<ModelMetadata>(
            MockBehavior.Loose,
            ModelMetadataIdentity.ForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel)));
        provider
            .Setup(m => m.GetMetadataForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel)))
            .Returns(metadata.Object);

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider.Object);

        // Act (do not throw)
        var idResult = helper.Id(expression: string.Empty);
        var idForResult = helper.IdFor(m => m);
        var idForModelResult = helper.IdForModel();
        var nameResult = helper.Name(expression: string.Empty);
        var nameForResult = helper.NameFor(m => m);
        var nameForModelResult = helper.NameForModel();

        // Assert
        // Only the ViewDataDictionary should do anything with metadata.
        provider.Verify(
            m => m.GetMetadataForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel)),
            Times.Exactly(1));
    }

    [Fact]
    public void IdAndNameHelpers_DoNotConsultMetadataOrMetadataProvider_ForProperty()
    {
        // Arrange
        var provider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
        var metadata = new Mock<ModelMetadata>(
            MockBehavior.Loose,
            ModelMetadataIdentity.ForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel)));
        provider
            .Setup(m => m.GetMetadataForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel)))
            .Returns(metadata.Object);

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider.Object);

        // Act (do not throw)
        var idResult = helper.Id("Property1");
        var idForResult = helper.IdFor(m => m.Property1);
        var nameResult = helper.Name("Property1");
        var nameForResult = helper.NameFor(m => m.Property1);

        // Assert
        // Only the ViewDataDictionary should do anything with metadata.
        provider.Verify(
            m => m.GetMetadataForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel)),
            Times.Exactly(1));
    }

    [Theory]
    [InlineData("A", "A")]
    [InlineData("A[0].B", "A_0__B")]
    [InlineData("A.B.C.D", "A_B_C_D")]
    public void IdAndName_ReturnExpression_EvenIfExpressionNotFound(string expression, string expectedId)
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var idResult = helper.Id(expression);
        var nameResult = helper.Name(expression);

        // Assert
        Assert.Equal(expectedId, idResult);
        Assert.Equal(expression, nameResult);
    }

    [Fact]
    public void IdForAndNameFor_ReturnEmpty_IfExpressionUnsupported()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var idResult = helper.IdFor(model => new { foo = "Bar" });
        var nameResult = helper.NameFor(model => new { foo = "Bar" });

        // Assert
        Assert.Empty(idResult);
        Assert.Empty(nameResult);
    }

    // expression, expected name, expected id
    public static TheoryData StaticExpressionNamesData
    {
        get
        {
            // Give expressions access to non-static fields and properties.
            var test = new HtmlHelperNameExtensionsTest();
            return test.ExpressionNamesData;
        }
    }

    // expression, expected name, expected id
    private TheoryData ExpressionNamesData
    {
        get
        {
            var collection = new List<string>();
            var nestedCollection = new List<string[]>();
            var index1 = 5;
            var index2 = 23;
            var unknownKey = "this is a dummy parameter value";

            return new TheoryData<Expression<Func<List<OuterClass>, string>>, string, string>
                {
                    { m => unknownKey, "unknownKey", "unknownKey" },
                    { m => collection[index1], "collection[5]", "collection_5_" },
                    { m => nestedCollection[index1][23], "nestedCollection[5][23]", "nestedCollection_5__23_" },
                    { m => nestedCollection[index1][index2], "nestedCollection[5][23]", "nestedCollection_5__23_" },
                    { m => nestedCollection[_index][Index], "nestedCollection[7][9]", "nestedCollection_7__9_" },
                    { m => nestedCollection[Index][StaticIndex], "nestedCollection[9][8]", "nestedCollection_9__8_" },
                    { m => _string, "_string", "zstring" },
                    { m => _collection[_index], "_collection[7]", "zcollection_7_" },
                    { m => _nestedCollection[_index][23], "_nestedCollection[7][23]", "znestedCollection_7__23_" },
                    { m => _nestedCollection[_index][index2], "_nestedCollection[7][23]", "znestedCollection_7__23_" },
                    { m => _nestedCollection[Index][_staticIndex], "_nestedCollection[9][6]", "znestedCollection_9__6_" },
                    { m => _nestedCollection[StaticIndex][_index], "_nestedCollection[8][7]", "znestedCollection_8__7_" },
                    { m => StringProperty, "StringProperty", "StringProperty" },
                    { m => Collection[Index], "Collection[9]", "Collection_9_" },
                    { m => NestedCollection[Index][23], "NestedCollection[9][23]", "NestedCollection_9__23_" },
                    { m => NestedCollection[Index][index2], "NestedCollection[9][23]", "NestedCollection_9__23_" },
                    { m => NestedCollection[_index][Index], "NestedCollection[7][9]", "NestedCollection_7__9_" },
                    { m => NestedCollection[Index][StaticIndex], "NestedCollection[9][8]", "NestedCollection_9__8_" },
                    { m => _staticCollection[_staticIndex], "_staticCollection[6]", "zstaticCollection_6_" },
                    { m => _staticCollection[Index], "_staticCollection[9]", "zstaticCollection_9_" },
                    { m => _staticCollection[_index], "_staticCollection[7]", "zstaticCollection_7_" },
                    { m => StaticCollection[StaticIndex], "StaticCollection[8]", "StaticCollection_8_" },
                    { m => StaticCollection[_staticIndex], "StaticCollection[6]", "StaticCollection_6_" },
                    { m => StaticCollection[index1], "StaticCollection[5]", "StaticCollection_5_" },
                    { m => m[index1].Inner.Name, "[5].Inner.Name", "z5__Inner_Name" },
                    { m => m[_staticIndex].Inner.Name, "[6].Inner.Name", "z6__Inner_Name" },
                    { m => m[_index].Inner.Name, "[7].Inner.Name", "z7__Inner_Name" },
                    { m => m[StaticIndex].Inner.Name, "[8].Inner.Name", "z8__Inner_Name" },
                    { m => m[Index].Inner.Name, "[9].Inner.Name", "z9__Inner_Name" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(StaticExpressionNamesData))]
    public void IdForAndNameFor_ReturnExpectedValues_WithVariablesInExpression(
        Expression<Func<List<OuterClass>, string>> expression,
        string expectedName,
        string expectedId)
    {
        // Arrange
        var model = new List<OuterClass>();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var idResult = helper.IdFor(expression);
        var nameResult = helper.NameFor(expression);

        // Assert
        Assert.Equal(expectedId, idResult);
        Assert.Equal(expectedName, nameResult);
    }

    [Fact]
    public void IdForAndNameFor_Throws_WhenParameterUsedAsIndexer()
    {
        // Arrange
        var collection = new List<string>();
        var index = 24;
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(index);
        var message = "The expression compiler was unable to evaluate the indexer expression 'm' because it " +
            "references the model parameter 'm' which is unavailable.";

        // Act & Assert
        ExceptionAssert.Throws<InvalidOperationException>(() => helper.IdFor(m => collection[m]), message);
        ExceptionAssert.Throws<InvalidOperationException>(() => helper.NameFor(m => collection[m]), message);
    }

    public sealed class InnerClass
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public sealed class OuterClass
    {
        public InnerClass Inner { get; set; }
    }
}
