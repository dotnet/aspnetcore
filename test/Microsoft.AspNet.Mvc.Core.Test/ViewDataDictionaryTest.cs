// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ViewDataDictionaryTest
    {
        [Fact]
        public void ConstructorWithOneParameterInitalizesMembers()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();

            // Act
            var viewData = new ViewDataDictionary(metadataProvider);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Null(viewData.Model);
            Assert.Null(viewData.ModelMetadata);
            Assert.Equal(0, viewData.Count);
        }

        [Fact]
        public void ConstructorInitalizesMembers()
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
            Assert.Null(viewData.ModelMetadata);
            Assert.Equal(0, viewData.Count);
        }

        [Fact]
        public void SetModelUsesPassedInModelMetadataProvider()
        {
            // Arrange
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider.Setup(m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(TestModel)))
                            .Returns(new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(TestModel)))
                            .Verifiable();
            var modelState = new ModelStateDictionary();
            var viewData = new TestViewDataDictionary(metadataProvider.Object, modelState);
            var model = new TestModel();

            // Act
            viewData.SetModelPublic(model);

            // Assert
            Assert.NotNull(viewData.ModelMetadata);
            metadataProvider.Verify();
        }

        [Fact]
        public void CopyConstructorInitalizesModelAndModelMetadataBasedOnSource()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var model = new TestModel();
            var source = new ViewDataDictionary(metadataProvider)
            {
                Model = model
            };
            source["foo"] = "bar";

            // Act
            var viewData = new ViewDataDictionary(source);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.NotSame(source.TemplateInfo, viewData.TemplateInfo);
            Assert.Same(model, viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData.ModelMetadata.ModelType);
            Assert.Equal("bar", viewData["foo"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Fact]
        public void CopyConstructorUsesPassedInModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var model = new TestModel();
            var source = new ViewDataDictionary(metadataProvider)
            {
                Model = "string model"
            };
            source["key1"] = "value1";

            // Act
            var viewData = new ViewDataDictionary(source, model);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Same(model, viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData.ModelMetadata.ModelType);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Fact]
        public void CopyConstructorDoesNotThrowOnNullModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary(metadataProvider);
            source["key1"] = "value1";

            // Act
            var viewData = new ViewDataDictionary(source, null);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Null(viewData.Model);
            Assert.Null(viewData.ModelMetadata);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Fact]
        public void CopyConstructorDoesNotThrowOnNullModel_WithValueTypeTModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary(metadataProvider);
            source["key1"] = "value1";

            // Act
            var viewData = new ViewDataDictionary<int>(source, null);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Throws<NullReferenceException>(() => viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(string[]))]
        [InlineData(typeof(Dictionary<string, object>))]
        public void CopyConstructors_CopyModelMetadata(Type type)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(() => null, type);
            var source = new ViewDataDictionary(metadataProvider)
            {
                ModelMetadata = metadata,
            };

            // Act
            var viewData1 = new ViewDataDictionary(source);
            var viewData2 = new ViewDataDictionary(source, model: null);

            // Assert
            Assert.Same(metadata, viewData1.ModelMetadata);
            Assert.Same(metadata, viewData2.ModelMetadata);
        }

        [Fact]
        public void CopyConstructors_IgnoreModelMetadata_IfForTypeObject()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(() => null, typeof(object));
            var source = new ViewDataDictionary(metadataProvider)
            {
                ModelMetadata = metadata,
            };

            // Act
            var viewData1 = new ViewDataDictionary(source);
            var viewData2 = new ViewDataDictionary(source, model: null);

            // Assert
            Assert.Null(viewData1.ModelMetadata);
            Assert.Null(viewData2.ModelMetadata);
        }

        [Theory]
        [InlineData(typeof(int), "test string", typeof(string))]
        [InlineData(typeof(string), 23, typeof(int))]
        [InlineData(typeof(IEnumerable<string>), new[] { "1", "2", "3", }, typeof(object[]))]
        [InlineData(typeof(List<string>), new[] { 1, 2, 3, }, typeof(object[]))]
        public void CopyConstructors_OverrideSourceMetadata_IfModelNonNull(
            Type sourceType,
            object instance,
            Type expectedType)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(() => null, sourceType);
            var source = new ViewDataDictionary(metadataProvider)
            {
                ModelMetadata = metadata,
            };

            // Act
            var viewData1 = new ViewDataDictionary(source)
            {
                Model = instance,
            };
            var viewData2 = new ViewDataDictionary(source, model: instance);

            // Assert
            Assert.NotNull(viewData1.ModelMetadata);
            Assert.Equal(expectedType, viewData1.ModelMetadata.ModelType);
            Assert.Equal(expectedType, viewData1.ModelMetadata.RealModelType);

            Assert.NotNull(viewData2.ModelMetadata);
            Assert.Equal(expectedType, viewData2.ModelMetadata.ModelType);
            Assert.Equal(expectedType, viewData2.ModelMetadata.RealModelType);
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
        public void EvalThrowsIfExpressionIsNullOrEmpty(string expression)
        {
            // Arrange
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(() => viewData.Eval(expression), "expression");
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
            viewData.Add("Foo", new Dictionary<string, object> { { "Bar", new Hashtable { { "Baz", "Quux" } } } });

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

        private class TestViewDataDictionary : ViewDataDictionary
        {
            public TestViewDataDictionary(IModelMetadataProvider modelMetadataProvider,
                                          ModelStateDictionary modelState)
                : base(modelMetadataProvider, modelState)
            {
            }

            public TestViewDataDictionary(ViewDataDictionary source)
                : base(source)
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
}