// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class ViewDataDictionaryTest
{
    [Fact]
    public void ConstructorWithOneParameterInitializesMembers()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();

        // Act
        var viewData = new ViewDataDictionary(metadataProvider);

        // Assert
        Assert.NotNull(viewData.ModelState);
        Assert.NotNull(viewData.TemplateInfo);
        Assert.Null(viewData.Model);
        Assert.NotNull(viewData.ModelMetadata);
        Assert.Empty(viewData);
    }

    [Fact]
    public void ConstructorInitializesMembers()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var modelState = new ModelStateDictionary();

        // Act
        var viewData = new ViewDataDictionary(metadataProvider, modelState);

        // Assert
        Assert.Same(modelState, viewData.ModelState);
        Assert.NotNull(viewData.TemplateInfo);
        Assert.Null(viewData.Model);
        Assert.NotNull(viewData.ModelMetadata);
        Assert.Empty(viewData);
    }

    [Fact]
    public void Constructor_GetsNewModelMetadata()
    {
        // Arrange
        var metadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
        metadataProvider
            .Setup(m => m.GetMetadataForType(typeof(object)))
            .Returns(new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)))
            .Verifiable();
        var modelState = new ModelStateDictionary();

        // Act
        var viewData = new ViewDataDictionary(metadataProvider.Object, modelState);

        // Assert
        Assert.NotNull(viewData.ModelMetadata);
        metadataProvider.Verify(m => m.GetMetadataForType(typeof(object)), Times.Once());
    }

    [Fact]
    public void SetModel_DoesNotGetNewModelMetadata_IfTypeCompatible()
    {
        // Arrange
        var metadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
        metadataProvider
            .Setup(m => m.GetMetadataForType(typeof(TestModel)))
            .Returns(new EmptyModelMetadataProvider().GetMetadataForType(typeof(TestModel)))
            .Verifiable();
        var viewData = new TestViewDataDictionary(metadataProvider.Object, typeof(TestModel));
        var model = new TestModel();

        // Act
        viewData.SetModelPublic(model);

        // Assert
        Assert.NotNull(viewData.ModelMetadata);
        metadataProvider.Verify(m => m.GetMetadataForType(typeof(TestModel)), Times.Once());
    }

    [Fact]
    public void SetModel_GetsNewModelMetadata_IfSourceTypeIsObject()
    {
        // Arrange
        var metadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
        metadataProvider
            .Setup(m => m.GetMetadataForType(typeof(object)))
            .Returns(new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)))
            .Verifiable();
        metadataProvider
            .Setup(m => m.GetMetadataForType(typeof(TestModel)))
            .Returns(new EmptyModelMetadataProvider().GetMetadataForType(typeof(TestModel)))
            .Verifiable();
        var viewData = new TestViewDataDictionary(metadataProvider.Object);
        var model = new TestModel();

        // Act
        viewData.SetModelPublic(model);

        // Assert
        Assert.NotNull(viewData.ModelMetadata);

        // For the constructor.
        metadataProvider.Verify(m => m.GetMetadataForType(typeof(object)), Times.Once());

        // For SetModel().
        metadataProvider.Verify(m => m.GetMetadataForType(typeof(TestModel)), Times.Once());
    }

    public static TheoryData<object, Type> IncompatibleModelData
    {
        get
        {
            // Small "anything but TestModel" grab bag of instances and expected types.
            return new TheoryData<object, Type>
                {
                    { true, typeof(bool) },
                    { 23, typeof(int) },
                    { 43.78, typeof(double) },
                    { "test string", typeof(string) },
                    { new List<int>(), typeof(List<int>) },
                    { new List<string>(), typeof(List<string>) },
                    { new List<TestModel>(), typeof(List<TestModel>) },
                };
        }
    }

    [Theory]
    [MemberData(nameof(IncompatibleModelData))]
    public void SetModel_Throws_IfModelIncompatibleWithDeclaredType(object model, Type expectedType)
    {
        // Arrange
        var viewData = new TestViewDataDictionary(new EmptyModelMetadataProvider(), typeof(TestModel));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => viewData.SetModelPublic(model));
        Assert.Equal(
            $"The model item passed into the ViewDataDictionary is of type '{ model.GetType() }', but this " +
            $"ViewDataDictionary instance requires a model item of type '{ typeof(TestModel) }'.",
            exception.Message);
    }

    public static TheoryData<object> EnumerableModelData
    {
        get
        {
            var model = new List<TestModel>()
                {
                    new TestModel(),
                    new TestModel()
                };

            return new TheoryData<object>
                {
                    { model.Select(t => t) },
                    { model.Where(t => t != null) },
                    { model.SelectMany(t => t.ToString()) },
                    { model.Take(2) },
                    { model.TakeWhile(t => t != null) },
                    { model.Union(model) }
                };
        }
    }

    [Theory]
    [MemberData(nameof(EnumerableModelData))]
    public void ModelSetter_DoesNotThrowOnEnumerableModel(object model)
    {
        // Arrange
        var vdd = new ViewDataDictionary(new EmptyModelMetadataProvider());

        // Act
        vdd.Model = model;

        // Assert
        Assert.Same(model, vdd.Model);
    }

    [Fact]
    public void CopyConstructorInitializesModelAndModelMetadataBasedOnSource()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var model = new TestModel();
        var source = new ViewDataDictionary(metadataProvider)
        {
            ModelExplorer = metadataProvider.GetModelExplorerForType(typeof(TestModel), model),
        };
        source["foo"] = "bar";
        source.TemplateInfo.HtmlFieldPrefix = "prefix";

        // Act
        var viewData = new ViewDataDictionary(source);

        // Assert
        Assert.NotNull(viewData.ModelState);
        Assert.NotNull(viewData.TemplateInfo);
        Assert.Equal("prefix", viewData.TemplateInfo.HtmlFieldPrefix);
        Assert.NotSame(source.TemplateInfo, viewData.TemplateInfo);
        Assert.Same(model, viewData.Model);
        Assert.NotNull(viewData.ModelMetadata);
        Assert.Equal(typeof(TestModel), viewData.ModelMetadata.ModelType);
        Assert.Same(source.ModelMetadata, viewData.ModelMetadata);
        Assert.Equal(source.Count, viewData.Count);
        Assert.Equal("bar", viewData["foo"]);
        Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
    }

    public static TheoryData<Type, object> CopyModelMetadataData
    {
        get
        {
            // Instances in this data set must have exactly the same type as the corresponding Type or be null.
            // Otherwise the copy constructor ignores the source ModelMetadata.
            return new TheoryData<Type, object>
                {
                    { typeof(int), 23 },
                    { typeof(ulong?), 24ul },
                    { typeof(ushort?), null },
                    { typeof(string), "hello" },
                    { typeof(string), null },
                    { typeof(List<string>), new List<string>() },
                    { typeof(string[]), new string[0] },
                    { typeof(Dictionary<string, object>), new Dictionary<string, object>() },
                };
        }
    }

    [Theory]
    [MemberData(nameof(CopyModelMetadataData))]
    public void CopyConstructor_CopiesModelMetadata(Type type, object instance)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var source = new ViewDataDictionary(metadataProvider)
        {
            Model = instance,
        };

        // Act
        var viewData = new ViewDataDictionary(source);

        // Assert
        Assert.Same(source.ModelMetadata, viewData.ModelMetadata);
    }

    [Fact]
    public void CopyConstructor_CopiesModelMetadata_ForTypeObject()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var source = new ViewDataDictionary(metadataProvider);

        // Act
        var viewData = new ViewDataDictionary(source);

        // Assert
        Assert.Same(source.ModelMetadata, viewData.ModelMetadata);
        Assert.Equal(typeof(object), viewData.ModelMetadata.ModelType);
    }

    [Theory]
    [InlineData(typeof(int), "test string", typeof(string))]
    [InlineData(typeof(string), 23, typeof(int))]
    [InlineData(typeof(IEnumerable<string>), new object[] { "1", "2", "3" }, typeof(object[]))]
    [InlineData(typeof(List<string>), new object[] { 1, 2, 3 }, typeof(object[]))]
    public void ModelSetter_UpdatesModelMetadata_IfModelIncompatibleWithSourceMetadata(
        Type sourceType,
        object model,
        Type expectedType)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var source = new ViewDataDictionary(metadataProvider)
        {
            ModelExplorer = metadataProvider.GetModelExplorerForType(sourceType, model: null),
        };
        var sourceMetadata = source.ModelMetadata;
        var viewData = new ViewDataDictionary(source);

        // Act
        viewData.Model = model;

        // Assert
        Assert.NotSame(source.ModelExplorer, viewData.ModelExplorer);
        Assert.NotSame(source.ModelMetadata, viewData.ModelMetadata);
        Assert.Equal(expectedType, viewData.ModelMetadata.ModelType);
    }

    [Theory]
    [InlineData(typeof(int), 23)]
    [InlineData(typeof(string), "test string")]
    [InlineData(typeof(IEnumerable<string>), new string[] { "1", "2", "3" })]
    public void ModelSetter_PreservesSourceMetadata_IfModelCompatible(Type sourceType, object model)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var source = new ViewDataDictionary(metadataProvider)
        {
            ModelExplorer = metadataProvider.GetModelExplorerForType(sourceType, model: null),
        };
        var viewData = new ViewDataDictionary(source);

        // Act
        viewData.Model = model;

        // Assert
        Assert.NotSame(source.ModelExplorer, viewData.ModelExplorer);
        Assert.Same(source.ModelMetadata, viewData.ModelMetadata);
    }

    [Fact]
    public void ModelSetter_SameType_UpdatesModelExplorer()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider)
        {
            Model = 3,
        };

        var originalMetadata = viewData.ModelMetadata;
        var originalExplorer = viewData.ModelExplorer;

        // Act
        viewData.Model = 5;

        // Assert
        Assert.NotNull(viewData.ModelMetadata);
        Assert.NotNull(viewData.ModelExplorer);
        Assert.Equal(5, viewData.Model);
        Assert.Equal(5, viewData.ModelExplorer.Model);
        Assert.Same(originalMetadata, viewData.ModelMetadata);
        Assert.NotSame(originalExplorer, viewData.ModelExplorer);
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(string))]
    public void ModelSetter_DifferentType_UpdatesModelMetadata(Type originalMetadataType)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(originalMetadataType);
        var explorer = new ModelExplorer(metadataProvider, metadata, model: null);
        var viewData = new TestViewDataDictionary(metadataProvider)
        {
            ModelExplorer = explorer,
        };

        // Act
        viewData.Model = true;

        // Assert
        Assert.NotNull(viewData.ModelExplorer);
        Assert.NotNull(viewData.ModelMetadata);
        Assert.NotSame(explorer, viewData.ModelExplorer);
        Assert.Equal(typeof(bool), viewData.ModelMetadata.ModelType);

        var model = Assert.IsType<bool>(viewData.Model);
        Assert.True(model);
    }

    [Fact]
    public void ModelSetter_SetNullableNonNull_UpdatesModelExplorer()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(bool?));
        var explorer = new ModelExplorer(metadataProvider, metadata, model: null);
        var viewData = new ViewDataDictionary(metadataProvider)
        {
            ModelExplorer = explorer,
        };

        // Act
        viewData.Model = true;

        // Assert
        Assert.NotNull(viewData.ModelMetadata);
        Assert.NotNull(viewData.ModelExplorer);
        Assert.Same(metadata, viewData.ModelMetadata);
        Assert.NotSame(explorer, viewData.ModelExplorer);
        Assert.Equal(viewData.Model, viewData.ModelExplorer.Model);

        var model = Assert.IsType<bool>(viewData.Model);
        Assert.True(model);
    }

    [Fact]
    public void ModelSetter_SetNonNullableToNull_Throws()
    {
        // Arrange
        var viewData = new TestViewDataDictionary(new EmptyModelMetadataProvider(), typeof(int));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => viewData.SetModelPublic(value: null));
        Assert.Equal(
            "The model item passed is null, but this ViewDataDictionary instance requires a non-null model item " +
            $"of type '{ typeof(int) }'.",
            exception.Message);
    }

    [Fact]
    public void ModelSetter_SameType_BoxedValueTypeUpdatesModelExplorer()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider)
        {
            Model = 3,
        };

        var originalMetadata = viewData.ModelMetadata;
        var originalExplorer = viewData.ModelExplorer;

        // Act
        viewData.Model = 3; // This is the same value, but it's in a different box.

        // Assert
        Assert.NotNull(viewData.ModelMetadata);
        Assert.NotNull(viewData.ModelExplorer);
        Assert.Equal(3, viewData.Model);
        Assert.Equal(3, viewData.ModelExplorer.Model);
        Assert.Same(originalMetadata, viewData.ModelMetadata);
        Assert.NotSame(originalExplorer, viewData.ModelExplorer);
    }

    [Fact]
    public void ModelSetter_SameModel_NoChanges()
    {
        // Arrange
        var model = "Hello";

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider)
        {
            Model = model,
        };

        var originalMetadata = viewData.ModelMetadata;
        var originalExplorer = viewData.ModelExplorer;

        // Act
        viewData.Model = model;

        // Assert
        Assert.NotNull(viewData.ModelMetadata);
        Assert.Equal("Hello", viewData.Model);
        Assert.Same(originalMetadata, viewData.ModelMetadata);
        Assert.Same(originalExplorer, viewData.ModelExplorer);
    }

    public static TheoryData<object, string, object> Eval_EvaluatesExpressionsData
    {
        get
        {
            return new TheoryData<object, string, object>
                {
                    {
                        new { Foo = "Bar" },
                        "Foo",
                        "Bar"
                    },
                    {
                        new { Foo = new Dictionary<string, object> { { "Bar", "Baz" } } },
                        "Foo.Bar",
                        "Baz"
                    },
                    {
                        new { Foo = new { Bar = "Baz" } },
                        "Foo.Bar",
                        "Baz"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(Eval_EvaluatesExpressionsData))]
    public void Eval_EvaluatesExpressions(object model, string expression, object expected)
    {
        // Arrange
        var viewData = GetViewDataDictionary(model);

        // Act
        var result = viewData.Eval(expression);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EvalReturnsNullIfExpressionDoesNotMatch()
    {
        // Arrange
        var model = new { Foo = new { Biz = "Baz" } };
        var viewData = GetViewDataDictionary(model);

        // Act
        var result = viewData.Eval("Foo.Bar");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EvalEvaluatesDictionaryThenModel()
    {
        // Arrange
        var model = new { Foo = "NotBar" };
        var viewData = GetViewDataDictionary(model);
        viewData.Add("Foo", "Bar");

        // Act
        var result = viewData.Eval("Foo");

        // Assert
        Assert.Equal("Bar", result);
    }

    [Fact]
    public void EvalReturnsValueJustAdded()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData.Add("Foo", "Blah");

        // Act
        var result = viewData.Eval("Foo");

        // Assert
        Assert.Equal("Blah", result);
    }

    [Fact]
    public void EvalWithCompoundExpressionReturnsIndexedValue()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData.Add("Foo.Bar", "Baz");

        // Act
        var result = viewData.Eval("Foo.Bar");

        // Assert
        Assert.Equal("Baz", result);
    }

    [Fact]
    public void EvalWithCompoundExpressionReturnsPropertyOfAddedObject()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData.Add("Foo", new { Bar = "Baz" });

        // Act
        var result = viewData.Eval("Foo.Bar");

        // Assert
        Assert.Equal("Baz", result);
    }

    [Fact]
    public void EvalWithCompoundIndexExpressionReturnsEval()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData.Add("Foo.Bar", new { Baz = "Quux" });

        // Act
        var result = viewData.Eval("Foo.Bar.Baz");

        // Assert
        Assert.Equal("Quux", result);
    }

    [Fact]
    public void EvalWithCompoundIndexAndCompoundExpressionReturnsValue()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData.Add("Foo.Bar", new { Baz = new { Blah = "Quux" } });

        // Act
        var result = viewData.Eval("Foo.Bar.Baz.Blah");

        // Assert
        Assert.Equal("Quux", result);
    }

    // Make sure that dict["foo.bar"] gets chosen before dict["foo"]["bar"]
    [Fact]
    public void EvalChoosesValueInDictionaryOverOtherValue()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
            {
                {  "Foo", new { Bar = "Not Baz" } },
                { "Foo.Bar", "Baz" }
            };

        // Act
        var result = viewData.Eval("Foo.Bar");

        // Assert
        Assert.Equal("Baz", result);
    }

    // Make sure that dict["foo.bar"]["baz"] gets chosen before dict["foo"]["bar"]["baz"]
    [Fact]
    public void EvalChoosesCompoundValueInDictionaryOverOtherValues()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
            {
                { "Foo", new { Bar = new { Baz = "Not Quux" } } },
                { "Foo.Bar", new { Baz = "Quux" } }
            };

        // Act
        var result = viewData.Eval("Foo.Bar.Baz");

        // Assert
        Assert.Equal("Quux", result);
    }

    // Make sure that dict["foo.bar"]["baz"] gets chosen before dict["foo"]["bar.baz"]
    [Fact]
    public void EvalChoosesCompoundValueInDictionaryOverOtherValuesWithCompoundProperty()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
            {
                { "Foo", new Person() },
                { "Foo.Bar", new { Baz = "Quux" } }
            };

        // Act
        var result = viewData.Eval("Foo.Bar.Baz");

        // Assert
        Assert.Equal("Quux", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Eval_ReturnsModel_IfExpressionIsNullOrEmpty(string expression)
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        var model = new object();
        viewData.Model = model;

        // Act
        var result = viewData.Eval(expression);

        // Assert
        Assert.Same(model, result);
    }

    [Fact]
    public void EvalWithCompoundExpressionAndDictionarySubExpressionChoosesDictionaryValue()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData.Add("Foo", new Dictionary<string, object> { { "Bar", "Baz" } });

        // Act
        var result = viewData.Eval("Foo.Bar");

        // Assert
        Assert.Equal("Baz", result);
    }

    [Fact]
    public void EvalWithDictionaryAndNoMatchReturnsNull()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData.Add("Foo", new Dictionary<string, object> { { "NotBar", "Baz" } });

        // Act
        var result = viewData.Eval("Foo.Bar");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EvalWithNestedDictionariesEvalCorrectly()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        var value = new Dictionary<string, object>
        {
            ["Bar"] = new Dictionary<string, string>
                {
                    { "Baz", "Quux" }
                }
        };
        viewData.Add("Foo", value);

        // Act
        var result = viewData.Eval("Foo.Bar.Baz");

        // Assert
        Assert.Equal("Quux", result);
    }

    [Fact]
    public void EvalFormatWithNullValueReturnsEmptyString()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        // Act
        var formattedValue = viewData.Eval("foo", "for{0}mat");

        // Assert
        Assert.Empty(formattedValue);
    }

    [Fact]
    public void EvalFormatWithEmptyFormatReturnsViewData()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData["foo"] = "value";

        // Act
        var formattedValue = viewData.Eval("foo", string.Empty);

        // Assert
        Assert.Equal("value", formattedValue);
    }

    [Fact]
    public void EvalFormatWithFormatReturnsFormattedViewData()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        viewData["foo"] = "value";

        // Act
        var formattedValue = viewData.Eval("foo", "for{0}mat");

        // Assert
        Assert.Equal("forvaluemat", formattedValue);
    }

    [Fact]
    public void EvalPropertyNamedModel()
    {
        // Arrange
        var model = new TheQueryStringParam
        {
            Name = "The Name",
            Value = "The Value",
            Model = "The Model",
        };
        var viewData = GetViewDataDictionary(model);
        viewData["Title"] = "Home Page";
        viewData["Message"] = "Welcome to ASP.NET MVC!";

        // Act
        var result = viewData.Eval("Model");

        // Assert
        Assert.Equal("The Model", result);
    }

    [Fact]
    public void EvalSubPropertyNamedValueInModel()
    {
        // Arrange
        var model = new TheQueryStringParam
        {
            Name = "The Name",
            Value = "The Value",
            Model = "The Model",
        };
        var viewData = GetViewDataDictionary(model);
        viewData["Title"] = "Home Page";
        viewData["Message"] = "Welcome to ASP.NET MVC!";

        // Act
        var result = viewData.Eval("Value");

        // Assert
        Assert.Equal("The Value", result);
    }

    private static ViewDataDictionary GetViewDataDictionary(object model)
    {
        return new ViewDataDictionary(new EmptyModelMetadataProvider())
        {
            Model = model
        };
    }

    private class TestModel
    {
    }

    private class Person
    {
        public string Name { get; set; }
    }

    private class TestViewDataDictionary : ViewDataDictionary
    {
        public TestViewDataDictionary(IModelMetadataProvider metadataProvider)
            : base(metadataProvider)
        {
        }

        public TestViewDataDictionary(IModelMetadataProvider metadataProvider, Type declaredModelType)
            : base(metadataProvider, declaredModelType)
        {
        }

        public void SetModelPublic(object value)
        {
            SetModel(value);
        }
    }

    private class TheQueryStringParam
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Model { get; set; }
    }
}
