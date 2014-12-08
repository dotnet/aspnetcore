// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class ModelBindingHelperTest
    {
        [Fact]
        public async Task TryUpdateModel_ReturnsFalse_IfBinderReturnsFalse()
        {
            // Arrange
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider.Setup(m => m.GetMetadataForType(null, It.IsAny<Type>()))
                            .Returns(new ModelMetadata(metadataProvider.Object, null, null, typeof(MyModel), null))
                            .Verifiable();

            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Returns(Task.FromResult(false));
            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    null,
                                                    Mock.Of<HttpContext>(),
                                                    new ModelStateDictionary(),
                                                    metadataProvider.Object,
                                                    GetCompositeBinder(binder.Object),
                                                    Mock.Of<IValueProvider>(),
                                                    Mock.Of<IModelValidatorProvider>());

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            metadataProvider.Verify();
        }

        [Fact]
        public async Task TryUpdateModel_ReturnsFalse_IfModelValidationFails()
        {
            // Arrange
            var expectedMessage = TestPlatformHelper.IsMono ? "The field MyProperty is invalid." :
                                                               "The MyProperty field is required.";
            var binders = new IModelBinder[]
            {
                new TypeConverterModelBinder(),
                new ComplexModelDtoModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider();
            var model = new MyModel();
            var modelStateDictionary = new ModelStateDictionary();
            var values = new Dictionary<string, object>
            {
                { "", null }
            };
            var valueProvider = new DictionaryBasedValueProvider<TestValueBinderMetadata>(values);

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    "",
                                                    Mock.Of<HttpContext>(),
                                                    modelStateDictionary,
                                                    new DataAnnotationsModelMetadataProvider(),
                                                    GetCompositeBinder(binders),
                                                    valueProvider,
                                                    validator);

            // Assert
            Assert.False(result);
            var error = Assert.Single(modelStateDictionary["MyProperty"].Errors);
            Assert.Equal(expectedMessage, error.ErrorMessage);
        }

        [Fact]
        public async Task TryUpdateModel_ReturnsTrue_IfModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new TypeConverterModelBinder(),
                new ComplexModelDtoModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider();
            var model = new MyModel { MyProperty = "Old-Value" };
            var modelStateDictionary = new ModelStateDictionary();
            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" }
            };
            var valueProvider = new DictionaryBasedValueProvider<TestValueBinderMetadata>(values);

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    "",
                                                    Mock.Of<HttpContext>(),
                                                    modelStateDictionary,
                                                    new DataAnnotationsModelMetadataProvider(),
                                                    GetCompositeBinder(binders),
                                                    valueProvider,
                                                    validator);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
        }

        [Fact]
        public async Task TryUpdateModel_UsingIncludePredicateOverload_ReturnsFalse_IfBinderReturnsFalse()
        {
            // Arrange
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider.Setup(m => m.GetMetadataForType(null, It.IsAny<Type>()))
                            .Returns(new ModelMetadata(metadataProvider.Object, null, null, typeof(MyModel), null))
                            .Verifiable();

            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Returns(Task.FromResult(false));
            var model = new MyModel();
            Func<ModelBindingContext, string, bool> includePredicate =
               (context, propertyName) => true;
            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    null,
                                                    Mock.Of<HttpContext>(),
                                                    new ModelStateDictionary(),
                                                    metadataProvider.Object,
                                                    GetCompositeBinder(binder.Object),
                                                    Mock.Of<IValueProvider>(),
                                                    Mock.Of<IModelValidatorProvider>(),
                                                    includePredicate);

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            Assert.Null(model.IncludedProperty);
            Assert.Null(model.ExcludedProperty);
            metadataProvider.Verify();
        }

        [Fact]
        public async Task TryUpdateModel_UsingIncludePredicateOverload_ReturnsTrue_ModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new TypeConverterModelBinder(),
                new ComplexModelDtoModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider();
            var model = new MyModel {
                MyProperty = "Old-Value",
                IncludedProperty = "Old-IncludedPropertyValue",
                ExcludedProperty = "Old-ExcludedPropertyValue"
            };

            var modelStateDictionary = new ModelStateDictionary();
            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" },
                { "IncludedProperty", "IncludedPropertyValue" },
                { "ExcludedProperty", "ExcludedPropertyValue" }
            };

            Func<ModelBindingContext, string, bool> includePredicate =
                (context, propertyName) =>
                                string.Equals(propertyName, "IncludedProperty", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(propertyName, "MyProperty", StringComparison.OrdinalIgnoreCase);

            var valueProvider = new DictionaryBasedValueProvider<TestValueBinderMetadata>(values);

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    "",
                                                    Mock.Of<HttpContext>(),
                                                    modelStateDictionary,
                                                    new DataAnnotationsModelMetadataProvider(),
                                                    GetCompositeBinder(binders),
                                                    valueProvider,
                                                    validator,
                                                    includePredicate);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
            Assert.Equal("IncludedPropertyValue", model.IncludedProperty);
            Assert.Equal("Old-ExcludedPropertyValue", model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModel_UsingIncludeExpressionOverload_ReturnsFalse_IfBinderReturnsFalse()
        {
            // Arrange
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider.Setup(m => m.GetMetadataForType(null, It.IsAny<Type>()))
                            .Returns(new ModelMetadata(metadataProvider.Object, null, null, typeof(MyModel), null))
                            .Verifiable();

            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Returns(Task.FromResult(false));
            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    null,
                                                    Mock.Of<HttpContext>(),
                                                    new ModelStateDictionary(),
                                                    metadataProvider.Object,
                                                    GetCompositeBinder(binder.Object),
                                                    Mock.Of<IValueProvider>(),
                                                    Mock.Of<IModelValidatorProvider>(),
                                                    m => m.IncludedProperty );

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            Assert.Null(model.IncludedProperty);
            Assert.Null(model.ExcludedProperty);
            metadataProvider.Verify();
        }

        [Fact]
        public async Task TryUpdateModel_UsingIncludeExpressionOverload_ReturnsTrue_ModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new TypeConverterModelBinder(),
                new ComplexModelDtoModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider();
            var model = new MyModel
            {
                MyProperty = "Old-Value",
                IncludedProperty = "Old-IncludedPropertyValue",
                ExcludedProperty = "Old-ExcludedPropertyValue"
            };

            var modelStateDictionary = new ModelStateDictionary();
            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" },
                { "IncludedProperty", "IncludedPropertyValue" },
                { "ExcludedProperty", "ExcludedPropertyValue" }
            };

            var valueProvider = new DictionaryBasedValueProvider<TestValueBinderMetadata>(values);

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    "",
                                                    Mock.Of<HttpContext>(),
                                                    modelStateDictionary,
                                                    new DataAnnotationsModelMetadataProvider(),
                                                    GetCompositeBinder(binders),
                                                    valueProvider,
                                                    validator,
                                                    m => m.IncludedProperty,
                                                    m => m.MyProperty);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
            Assert.Equal("IncludedPropertyValue", model.IncludedProperty);
            Assert.Equal("Old-ExcludedPropertyValue", model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModel_UsingDefaultIncludeOverload_IncludesAllProperties()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new TypeConverterModelBinder(),
                new ComplexModelDtoModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider();
            var model = new MyModel
            {
                MyProperty = "Old-Value",
                IncludedProperty = "Old-IncludedPropertyValue",
                ExcludedProperty = "Old-ExcludedPropertyValue"
            };

            var modelStateDictionary = new ModelStateDictionary();
            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" },
                { "IncludedProperty", "IncludedPropertyValue" },
                { "ExcludedProperty", "ExcludedPropertyValue" }
            };

            var valueProvider = new DictionaryBasedValueProvider<TestValueBinderMetadata>(values);

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                                                    model,
                                                    "",
                                                    Mock.Of<HttpContext>(),
                                                    modelStateDictionary,
                                                    new DataAnnotationsModelMetadataProvider(),
                                                    GetCompositeBinder(binders),
                                                    valueProvider,
                                                    validator);

            // Assert
            // Includes everything.
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
            Assert.Equal("IncludedPropertyValue", model.IncludedProperty);
            Assert.Equal("ExcludedPropertyValue", model.ExcludedProperty);
        }

        [Fact]
        public void GetPropertyName_PropertyMemberAccessReturnsPropertyName()
        {
            // Arrange
            Expression<Func<User, object>> expression = m => m.Address;

            // Act
            var propertyName = ModelBindingHelper.GetPropertyName(expression.Body);

            // Assert
            Assert.Equal(nameof(User.Address), propertyName);
        }

        [Fact]
        public void GetPropertyName_ChainedExpression_Throws()
        {
            // Arrange
            Expression<Func<User, object>> expression = m => m.Address.Street;

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                        ModelBindingHelper.GetPropertyName(expression.Body));

            Assert.Equal(string.Format("The passed expression of expression node type '{0}' is invalid." +
                                       " Only simple member access expressions for model properties are supported.",
                                        expression.Body.NodeType),
                         ex.Message);
        }

        public static IEnumerable<object[]> InvalidExpressionDataSet
        {
            get
            {
                Expression<Func<User, object>> expression = m => new Func<User>(() => m);
                yield return new object[] { expression }; // lambda expression.

                expression = m => m.Save();
                yield return new object[] { expression }; // method call expression.

                expression = m => m.Friends[0]; // ArrayIndex expression.
                yield return new object[] { expression };

                expression = m => m.Colleagues[0]; // Indexer expression.
                yield return new object[] { expression };

                expression = m => m; // Parameter expression.
                yield return new object[] { expression };

                object someVariable = "something";
                expression = m => someVariable; // Variable accessor.
                yield return new object[] { expression };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidExpressionDataSet))]
        public void GetPropertyName_ExpressionsOtherThanMemberAccess_Throws(Expression<Func<User, object>> expression)
        {
            // Arrange Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                        ModelBindingHelper.GetPropertyName(expression.Body));

            Assert.Equal(string.Format("The passed expression of expression node type '{0}' is invalid."+
                                       " Only simple member access expressions for model properties are supported.",
                                        expression.Body.NodeType),
                         ex.Message);
        }

        [Fact]
        public void GetPropertyName_NonParameterBasedExpression_Throws()
        {
            // Arrange
            var someUser = new User();

            // PropertyAccessor with a property name invalid as it originates from a variable accessor.
            Expression<Func<User, object>> expression = m => someUser.Address;

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                        ModelBindingHelper.GetPropertyName(expression.Body));

            Assert.Equal(string.Format("The passed expression of expression node type '{0}' is invalid." +
                                       " Only simple member access expressions for model properties are supported.",
                                        expression.Body.NodeType),
                        ex.Message);
        }

        [Fact]
        public void GetPropertyName_TopLevelCollectionIndexer_Throws()
        {
            // Arrange
            Expression<Func<List<User>, object>> expression = m => m[0];

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                        ModelBindingHelper.GetPropertyName(expression.Body));

            Assert.Equal(string.Format("The passed expression of expression node type '{0}' is invalid." +
                                       " Only simple member access expressions for model properties are supported.",
                                        expression.Body.NodeType),
                         ex.Message);
        }

        [Fact]
        public void GetPropertyName_FieldExpression_Throws()
        {
            // Arrange
            Expression<Func<User, object>> expression = m => m._userId;

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                        ModelBindingHelper.GetPropertyName(expression.Body));

            Assert.Equal(string.Format("The passed expression of expression node type '{0}' is invalid." +
                                       " Only simple member access expressions for model properties are supported.",
                                        expression.Body.NodeType),
                         ex.Message);
        }

        private static IModelBinder GetCompositeBinder(params IModelBinder[] binders)
        {
            return new CompositeModelBinder(binders);
        }

        public class User
        {
            public string _userId;

            public Address Address { get; set; }

            public User[] Friends { get; set; }

            public List<User> Colleagues { get; set; }

            public bool IsReadOnly
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public User Save()
            {
                return this;
            }
        }

        public class Address
        {
            public string Street { get; set; }
        }

        private class MyModel
        {
            [Required]
            public string MyProperty { get; set; }

            public string IncludedProperty { get; set; }

            public string ExcludedProperty { get; set; }
        }

        private class TestValueBinderMetadata : IValueProviderMetadata
        {
        }
    }
}
#endif
