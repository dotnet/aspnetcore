// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.Internal;
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
            Assert.NotNull(viewData.ModelMetadata);
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
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(0, viewData.Count);
        }

        [Fact]
        public void SetModelUsesPassedInModelMetadataProvider()
        {
            // Arrange
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider
                .Setup(m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(object)))
                .Returns(new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(object)))
                .Verifiable();
            metadataProvider
                .Setup(m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(TestModel)))
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

        // When SetModel is called, only GetMetadataForType from MetadataProvider is expected to be called.
        [Fact]
        public void SetModelCallsGetMetadataForTypeExactlyOnce()
        {
            // Arrange
            var metadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            metadataProvider
                .Setup(m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(object)))
                .Returns(new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(object)))
                .Verifiable();
            metadataProvider
                .Setup(m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(TestModel)))
                .Returns(new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(TestModel)))
                .Verifiable();
            var modelState = new ModelStateDictionary();
            var viewData = new TestViewDataDictionary(metadataProvider.Object, modelState);
            var model = new TestModel();

            // Act
            viewData.SetModelPublic(model);

            // Assert
            Assert.NotNull(viewData.ModelMetadata);
            // Verifies if the GetMetadataForType is called only once.
            metadataProvider.Verify(
                m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(object)), Times.Once());
            // Verifies if GetMetadataForProperties and GetMetadataForProperty is not called.
            metadataProvider.Verify(
                m => m.GetMetadataForProperties(It.IsAny<Func<object>>(), typeof(object)), Times.Never());
            metadataProvider.Verify(
                m => m.GetMetadataForProperty(
                    It.IsAny<Func<object>>(), typeof(object), It.IsAny<string>()), Times.Never());
        }

        public static TheoryData<object> SetModelData
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
        [MemberData(nameof(SetModelData))]
        public void SetModelDoesNotThrowOnEnumerableModel(object model)
        {
            // Arrange
            var vdd = new ViewDataDictionary(new EmptyModelMetadataProvider());
            
            // Act
            vdd.Model = model;

            // Assert
            Assert.Same(model, vdd.Model);
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

        [Fact]
        public void CopyConstructorUsesPassedInModel_DifferentModels()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var model = new TestModel();
            var source = new ViewDataDictionary(metadataProvider)
            {
                Model = "string model"
            };
            source["key1"] = "value1";
            source.TemplateInfo.HtmlFieldPrefix = "prefix";

            // Act
            var viewData = new ViewDataDictionary(source, model);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.Equal("prefix", viewData.TemplateInfo.HtmlFieldPrefix);
            Assert.NotSame(source.TemplateInfo, viewData.TemplateInfo);
            Assert.Same(model, viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData.ModelMetadata.ModelType);
            Assert.NotSame(source.ModelMetadata, viewData.ModelMetadata);
            Assert.Equal(source.Count, viewData.Count);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        [Fact]
        public void CopyConstructorUsesPassedInModel_SameModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var model = new TestModel();
            var source = new ViewDataDictionary(metadataProvider)
            {
                Model = model
            };
            source["key1"] = "value1";

            // Act
            var viewData = new ViewDataDictionary(source, model);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.NotSame(source.TemplateInfo, viewData.TemplateInfo);
            Assert.Same(model, viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(typeof(TestModel), viewData.ModelMetadata.ModelType);
            Assert.Same(source.ModelMetadata, viewData.ModelMetadata);
            Assert.Equal(source.Count, viewData.Count);
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
            var viewData = new ViewDataDictionary(source, model: null);

            // Assert
            Assert.NotNull(viewData.ModelState);
            Assert.NotNull(viewData.TemplateInfo);
            Assert.NotSame(source.TemplateInfo, viewData.TemplateInfo);
            Assert.Null(viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal(typeof(object), viewData.ModelMetadata.ModelType);
            Assert.Same(source.ModelMetadata, viewData.ModelMetadata);
            Assert.Equal(source.Count, viewData.Count);
            Assert.Equal("value1", viewData["key1"]);
            Assert.IsType<CopyOnWriteDictionary<string, object>>(viewData.Data);
        }

        public static TheoryData<Type, object> CopyModelMetadataData
        {
            get
            {
                // Instances in this data set must have exactly the same type as the corresponding Type. Otherwise
                // the copy constructor ignores the source ModelMetadata.
                return new TheoryData<Type, object>
                {
                    { typeof(int), 23 },
                    { typeof(string), "hello" },
                    { typeof(List<string>), new List<string>() },
                    { typeof(string[]), new string[0] },
                    { typeof(Dictionary<string, object>), new Dictionary<string, object>() },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CopyModelMetadataData))]
        public void CopyConstructors_CopyModelMetadata(Type type, object instance)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var source = new ViewDataDictionary(metadataProvider)
            {
                Model = instance,
            };

            // Act
            var viewData1 = new ViewDataDictionary(source);
            var viewData2 = new ViewDataDictionary(source, model: instance);

            // Assert
            Assert.Same(source.ModelMetadata, viewData1.ModelMetadata);
            Assert.Same(source.ModelMetadata, viewData2.ModelMetadata);
        }

        [Fact]
        public void CopyConstructors_CopyModelMetadata_ForTypeObject()
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
            Assert.Same(metadata, viewData1.ModelMetadata);
            Assert.Same(metadata, viewData2.ModelMetadata);
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

        [Fact]
        public void ModelSetter_UpdatesModelMetadata()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(metadataProvider)
            {
                Model = "same-string",
            };
            var originalMetadata = viewData.ModelMetadata;

            // Act
            viewData.Model = "same-string";

            // Assert
            Assert.NotNull(viewData.ModelMetadata);
            Assert.Equal("same-string", viewData.Model);
            Assert.Equal("same-string", viewData.ModelMetadata.Model);
            Assert.Same(originalMetadata.ModelType, viewData.ModelMetadata.ModelType);
            Assert.NotSame(originalMetadata, viewData.ModelMetadata);
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

        private class Person
        {
            public string Name { get; set; }
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