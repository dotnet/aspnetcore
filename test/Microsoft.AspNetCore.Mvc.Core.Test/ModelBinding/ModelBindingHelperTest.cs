// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ModelBindingHelperTest
    {
        [Fact]
        public async Task TryUpdateModel_ReturnsFalse_IfBinderIsUnsuccessful()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new StubModelBinder(ModelBindingResult.Failed());
            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                string.Empty,
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetModelBinderFactory(binder),
                Mock.Of<IValueProvider>(),
                new Mock<IObjectModelValidator>(MockBehavior.Strict).Object);

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
        }

        [Fact]
        public async Task TryUpdateModel_ReturnsFalse_IfModelValidationFails()
        {
            // Arrange
            var binderProviders = new IModelBinderProvider[]
            {
                new SimpleTypeModelBinderProvider(),
                new ComplexTypeModelBinderProvider(),
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);
            var model = new MyModel();

            var values = new Dictionary<string, object>
            {
                { "", null }
            };
            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            var actionContext = new ActionContext() { HttpContext = new DefaultHttpContext() };
            var modelState = actionContext.ModelState;

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                "",
                actionContext,
                metadataProvider,
                GetModelBinderFactory(binderProviders),
                valueProvider,
                new DefaultObjectValidator(metadataProvider, new[] { validator }));

            // Assert
            Assert.False(result);
            var error = Assert.Single(modelState["MyProperty"].Errors);
            Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("MyProperty"), error.ErrorMessage);
        }

        [Fact]
        public async Task TryUpdateModel_ReturnsTrue_IfModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binderProviders = new IModelBinderProvider[]
            {
                new SimpleTypeModelBinderProvider(),
                new ComplexTypeModelBinderProvider(),
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);
            var model = new MyModel { MyProperty = "Old-Value" };

            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" }
            };
            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                "",
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetModelBinderFactory(binderProviders),
                valueProvider,
                new DefaultObjectValidator(metadataProvider, new[] { validator }));

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
        }

        [Fact]
        public async Task TryUpdateModel_UsingPropertyFilterOverload_ReturnsFalse_IfBinderIsUnsuccessful()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new StubModelBinder(ModelBindingResult.Failed());
            var model = new MyModel();
            Func<ModelMetadata, bool> propertyFilter = (m) => true;

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                string.Empty,
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetModelBinderFactory(binder),
                Mock.Of<IValueProvider>(),
                new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                propertyFilter);

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            Assert.Null(model.IncludedProperty);
            Assert.Null(model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModel_UsingPropertyFilterOverload_ReturnsTrue_ModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binderProviders = new IModelBinderProvider[]
            {
                new SimpleTypeModelBinderProvider(),
                new ComplexTypeModelBinderProvider(),
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);
            var model = new MyModel
            {
                MyProperty = "Old-Value",
                IncludedProperty = "Old-IncludedPropertyValue",
                ExcludedProperty = "Old-ExcludedPropertyValue"
            };

            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" },
                { "IncludedProperty", "IncludedPropertyValue" },
                { "ExcludedProperty", "ExcludedPropertyValue" }
            };

            Func<ModelMetadata, bool> propertyFilter = (m) =>
                string.Equals(m.PropertyName, "IncludedProperty", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.PropertyName, "MyProperty", StringComparison.OrdinalIgnoreCase);

            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                "",
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetModelBinderFactory(binderProviders),
                valueProvider,
                new DefaultObjectValidator(metadataProvider, new[] { validator }),
                propertyFilter);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
            Assert.Equal("IncludedPropertyValue", model.IncludedProperty);
            Assert.Equal("Old-ExcludedPropertyValue", model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModel_UsingIncludeExpressionOverload_ReturnsFalse_IfBinderIsUnsuccessful()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new StubModelBinder(ModelBindingResult.Failed());
            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                string.Empty,
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetModelBinderFactory(binder),
                Mock.Of<IValueProvider>(),
                new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                m => m.IncludedProperty);

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            Assert.Null(model.IncludedProperty);
            Assert.Null(model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModel_UsingIncludeExpressionOverload_ReturnsTrue_ModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binderProviders = new IModelBinderProvider[]
            {
                new SimpleTypeModelBinderProvider(),
                new ComplexTypeModelBinderProvider(),
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);
            var model = new MyModel
            {
                MyProperty = "Old-Value",
                IncludedProperty = "Old-IncludedPropertyValue",
                ExcludedProperty = "Old-ExcludedPropertyValue"
            };

            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" },
                { "IncludedProperty", "IncludedPropertyValue" },
                { "ExcludedProperty", "ExcludedPropertyValue" }
            };

            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                "",
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                TestModelMetadataProvider.CreateDefaultProvider(),
                GetModelBinderFactory(binderProviders),
                valueProvider,
                new DefaultObjectValidator(metadataProvider, new[] { validator }),
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
            var binderProviders = new IModelBinderProvider[]
            {
                new SimpleTypeModelBinderProvider(),
                new ComplexTypeModelBinderProvider(),
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);
            var model = new MyModel
            {
                MyProperty = "Old-Value",
                IncludedProperty = "Old-IncludedPropertyValue",
                ExcludedProperty = "Old-ExcludedPropertyValue"
            };

            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" },
                { "IncludedProperty", "IncludedPropertyValue" },
                { "ExcludedProperty", "ExcludedPropertyValue" }
            };

            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                "",
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetModelBinderFactory(binderProviders),
                valueProvider,
                new DefaultObjectValidator(metadataProvider, new[] { validator }));

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

            Assert.Equal(
                $"The passed expression of expression node type '{expression.Body.NodeType}' is invalid." +
                " Only simple member access expressions for model properties are supported.",
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

            Assert.Equal(
                $"The passed expression of expression node type '{expression.Body.NodeType}' is invalid." +
                " Only simple member access expressions for model properties are supported.",
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

            Assert.Equal(
                $"The passed expression of expression node type '{expression.Body.NodeType}' is invalid." +
                " Only simple member access expressions for model properties are supported.",
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

            Assert.Equal(
                $"The passed expression of expression node type '{expression.Body.NodeType}' is invalid." +
                " Only simple member access expressions for model properties are supported.",
                ex.Message);
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_PropertyFilterOverload_ReturnsFalse_IfBinderIsUnsuccessful()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new StubModelBinder(ModelBindingResult.Failed());
            var model = new MyModel();
            Func<ModelMetadata, bool> propertyFilter = (m) => true;

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                model.GetType(),
                prefix: "",
                actionContext: new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider: metadataProvider,
                modelBinderFactory: GetModelBinderFactory(binder),
                valueProvider: Mock.Of<IValueProvider>(),
                objectModelValidator: new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                propertyFilter: propertyFilter);

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            Assert.Null(model.IncludedProperty);
            Assert.Null(model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_PropertyFilterOverload_ReturnsTrue_ModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binderProviders = new IModelBinderProvider[]
            {
                new SimpleTypeModelBinderProvider(),
                new ComplexTypeModelBinderProvider(),
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);
            var model = new MyModel
            {
                MyProperty = "Old-Value",
                IncludedProperty = "Old-IncludedPropertyValue",
                ExcludedProperty = "Old-ExcludedPropertyValue"
            };

            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" },
                { "IncludedProperty", "IncludedPropertyValue" },
                { "ExcludedProperty", "ExcludedPropertyValue" }
            };

            Func<ModelMetadata, bool> propertyFilter = (m) =>
                string.Equals(m.PropertyName, "IncludedProperty", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.PropertyName, "MyProperty", StringComparison.OrdinalIgnoreCase);

            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                model.GetType(),
                "",
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetModelBinderFactory(binderProviders),
                valueProvider,
                new DefaultObjectValidator(metadataProvider, new[] { validator }),
                propertyFilter);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
            Assert.Equal("IncludedPropertyValue", model.IncludedProperty);
            Assert.Equal("Old-ExcludedPropertyValue", model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_ModelTypeOverload_ReturnsFalse_IfBinderIsUnsuccessful()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new StubModelBinder(ModelBindingResult.Failed());

            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                modelType: model.GetType(),
                prefix: "",
                actionContext: new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider: metadataProvider,
                modelBinderFactory: GetModelBinderFactory(binder.Object),
                valueProvider: Mock.Of<IValueProvider>(),
                objectModelValidator: new Mock<IObjectModelValidator>(MockBehavior.Strict).Object);

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_ModelTypeOverload_ReturnsTrue_IfModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binderProviders = new IModelBinderProvider[]
            {
                new SimpleTypeModelBinderProvider(),
                new ComplexTypeModelBinderProvider(),
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);
            var model = new MyModel { MyProperty = "Old-Value" };

            var values = new Dictionary<string, object>
            {
                { "", null },
                { "MyProperty", "MyPropertyValue" }
            };
            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                model.GetType(),
                "",
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetModelBinderFactory(binderProviders),
                valueProvider,
                new DefaultObjectValidator(metadataProvider, new[] { validator }));

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
        }

        [Fact]
        public async Task TryUpdataModel_ModelTypeDifferentFromModel_Throws()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();

            var binder = new StubModelBinder();
            var model = new MyModel();
            Func<ModelMetadata, bool> propertyFilter = (m) => true;

            var modelName = model.GetType().FullName;
            var userName = typeof(User).FullName;
            var expectedMessage = $"The model's runtime type '{modelName}' is not assignable to the type '{userName}'.";

            // Act & Assert
            var exception = await ExceptionAssert.ThrowsArgumentAsync(
                () => ModelBindingHelper.TryUpdateModelAsync(
                    model,
                    typeof(User),
                    "",
                    new ActionContext() { HttpContext = new DefaultHttpContext() },
                    metadataProvider,
                    GetModelBinderFactory(binder.Object),
                    Mock.Of<IValueProvider>(),
                    new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                    propertyFilter),
                "modelType",
                expectedMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ClearValidationState_ForComplexTypeModel_EmptyModelKey(string modelKey)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForType(typeof(Product));

            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError("Name", "MyProperty invalid.");
            dictionary.AddModelError("Id", "Id invalid.");
            dictionary.AddModelError("Id", "Id is required.");
            dictionary.MarkFieldValid("Category");
            dictionary.AddModelError("Unrelated", "Unrelated is required.");

            // Act
            ModelBindingHelper.ClearValidationStateForModel(modelMetadata, dictionary, modelKey);

            // Assert
            Assert.Empty(dictionary["Name"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Name"].ValidationState);
            Assert.Empty(dictionary["Id"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Id"].ValidationState);
            Assert.Empty(dictionary["Category"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Category"].ValidationState);

            Assert.Single(dictionary["Unrelated"].Errors);
            Assert.Equal(ModelValidationState.Invalid, dictionary["Unrelated"].ValidationState);
        }

        // Not a wholly realistic scenario, but testing it regardless.
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ClearValidationState_ForSimpleTypeModel_EmptyModelKey(string modelKey)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForType(typeof(string));

            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError(string.Empty, "MyProperty invalid.");
            dictionary.AddModelError("Unrelated", "Unrelated is required.");

            // Act
            ModelBindingHelper.ClearValidationStateForModel(modelMetadata, dictionary, modelKey);

            // Assert
            Assert.Empty(dictionary[string.Empty].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary[string.Empty].ValidationState);

            Assert.Single(dictionary["Unrelated"].Errors);
            Assert.Equal(ModelValidationState.Invalid, dictionary["Unrelated"].ValidationState);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ClearValidationState_ForCollectionsModel_EmptyModelKey(string modelKey)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForType(typeof(List<Product>));

            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError("[0].Name", "Name invalid.");
            dictionary.AddModelError("[0].Id", "Id invalid.");
            dictionary.AddModelError("[0].Id", "Id required.");
            dictionary.MarkFieldValid("[0].Category");

            dictionary.MarkFieldValid("[1].Name");
            dictionary.MarkFieldValid("[1].Id");
            dictionary.AddModelError("[1].Category", "Category invalid.");
            dictionary.AddModelError("Unrelated", "Unrelated is required.");

            // Act
            ModelBindingHelper.ClearValidationStateForModel(modelMetadata, dictionary, modelKey);

            // Assert
            Assert.Empty(dictionary["[0].Name"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[0].Name"].ValidationState);
            Assert.Empty(dictionary["[0].Id"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[0].Id"].ValidationState);
            Assert.Empty(dictionary["[0].Category"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[0].Category"].ValidationState);
            Assert.Empty(dictionary["[1].Name"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[1].Name"].ValidationState);
            Assert.Empty(dictionary["[1].Id"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[1].Id"].ValidationState);
            Assert.Empty(dictionary["[1].Category"].Errors);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[1].Category"].ValidationState);

            Assert.Single(dictionary["Unrelated"].Errors);
            Assert.Equal(ModelValidationState.Invalid, dictionary["Unrelated"].ValidationState);
        }

        [Theory]
        [InlineData("product")]
        [InlineData("product.Name")]
        [InlineData("product.Order[0].Name")]
        [InlineData("product.Order[0].Address.Street")]
        [InlineData("product.Category.Name")]
        [InlineData("product.Order")]
        public void ClearValidationState_ForComplexModel_NonEmptyModelKey(string prefix)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForType(typeof(Product));

            var dictionary = new ModelStateDictionary();
            dictionary.AddModelError("product.Name", "Name invalid.");
            dictionary.AddModelError("product.Id", "Id invalid.");
            dictionary.AddModelError("product.Id", "Id required.");
            dictionary.MarkFieldValid("product.Category");
            dictionary.MarkFieldValid("product.Category.Name");
            dictionary.AddModelError("product.Order[0].Name", "Order name invalid.");
            dictionary.AddModelError("product.Order[0].Address.Street", "Street invalid.");
            dictionary.MarkFieldValid("product.Order[1].Name");
            dictionary.AddModelError("product.Order[0]", "Order invalid.");

            // Act
            ModelBindingHelper.ClearValidationStateForModel(modelMetadata, dictionary, prefix);

            // Assert
            foreach (var entry in dictionary.Keys)
            {
                if (entry.StartsWith(prefix))
                {
                    Assert.Empty(dictionary[entry].Errors);
                    Assert.Equal(ModelValidationState.Unvalidated, dictionary[entry].ValidationState);
                }
            }
        }

        public static ModelBinderFactory GetModelBinderFactory(IModelBinder binder)
        {
            var binderProvider = new Mock<IModelBinderProvider>();
            binderProvider
                .Setup(p => p.GetBinder(It.IsAny<ModelBinderProviderContext>()))
                .Returns(binder);

            return TestModelBinderFactory.Create(binderProvider.Object);
        }

        private static ModelBinderFactory GetModelBinderFactory(params IModelBinderProvider[] providers)
        {
            return TestModelBinderFactory.CreateDefault(providers);
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

        private class Product
        {
            public string Name { get; set; }
            public int Id { get; set; }
            public Category Category { get; set; }
            public List<Order> Orders { get; set; }
        }

        public class Category
        {
            public string Name { get; set; }
        }

        public class Order
        {
            public string Name { get; set; }
            public Address Address { get; set; }
        }

        [Fact]
        public void ConvertTo_ReturnsNullForReferenceTypes_WhenValueIsNull()
        {
            var convertedValue = ModelBindingHelper.ConvertTo(value: null, type: typeof(string), culture: null);
            Assert.Null(convertedValue);
        }

        [Fact]
        public void ConvertTo_ReturnsDefaultForValueTypes_WhenValueIsNull()
        {
            var convertedValue = ModelBindingHelper.ConvertTo(value: null, type: typeof(int), culture: null);
            Assert.Equal(0, convertedValue);
        }

        [Fact]
        public void ConvertToCanConvertArraysToSingleElements()
        {
            // Arrange
            var value = new int[] { 1, 20, 42 };

            // Act
            var converted = ModelBindingHelper.ConvertTo(value, typeof(string), culture: null);

            // Assert
            Assert.Equal("1", converted);
        }

        [Fact]
        public void ConvertToCanConvertSingleElementsToArrays()
        {
            // Arrange
            var value = 42;

            // Act
            var converted = ModelBindingHelper.ConvertTo<string[]>(value, culture: null);

            // Assert
            Assert.NotNull(converted);
            var result = Assert.Single(converted);
            Assert.Equal("42", result);
        }

        [Fact]
        public void ConvertToCanConvertSingleElementsToSingleElements()
        {
            // Arrange

            // Act
            var converted = ModelBindingHelper.ConvertTo<string>(42, culture: null);

            // Assert
            Assert.NotNull(converted);
            Assert.Equal("42", converted);
        }

        [Fact]
        public void ConvertingNullStringToNullableIntReturnsNull()
        {
            // Arrange

            // Act
            var returned = ModelBindingHelper.ConvertTo<int?>(value: null, culture: null);

            // Assert
            Assert.Null(returned);
        }

        [Fact]
        public void ConvertingWhiteSpaceStringToNullableIntReturnsNull()
        {
            // Arrange
            var original = " ";

            // Act
            var returned = ModelBindingHelper.ConvertTo<int?>(original, culture: null);

            // Assert
            Assert.Null(returned);
        }

        [Fact]
        public void ConvertToReturnsNullIfArrayElementValueIsNull()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new string[] { null }, typeof(int), culture: null);

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsNullIfTryingToConvertEmptyArrayToSingleElement()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new int[0], typeof(int), culture: null);

            // Assert
            Assert.Null(outValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t \r\n ")]
        public void ConvertToReturnsNullIfTrimmedValueIsEmptyString(object value)
        {
            // Arrange
            // Act
            var outValue = ModelBindingHelper.ConvertTo(value, typeof(int), culture: null);

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsNull_IfConvertingNullToArrayType()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(value: null, type: typeof(int[]), culture: null);

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsIntegerAndDestinationTypeIsEnum()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { 1 }, typeof(IntEnum), culture: null);

            // Assert
            Assert.Equal(IntEnum.Value1, outValue);
        }

        [Theory]
        [InlineData(1, typeof(IntEnum), IntEnum.Value1)]
        [InlineData(1L, typeof(LongEnum), LongEnum.Value1)]
        [InlineData(long.MaxValue, typeof(LongEnum), LongEnum.MaxValue)]
        [InlineData(1U, typeof(UnsignedIntEnum), UnsignedIntEnum.Value1)]
        [InlineData(1UL, typeof(IntEnum), IntEnum.Value1)]
        [InlineData((byte)1, typeof(ByteEnum), ByteEnum.Value1)]
        [InlineData(byte.MaxValue, typeof(ByteEnum), ByteEnum.MaxValue)]
        [InlineData((sbyte)1, typeof(ByteEnum), ByteEnum.Value1)]
        [InlineData((short)1, typeof(IntEnum), IntEnum.Value1)]
        [InlineData((ushort)1, typeof(IntEnum), IntEnum.Value1)]
        [InlineData(int.MaxValue, typeof(IntEnum?), IntEnum.MaxValue)]
        [InlineData(null, typeof(IntEnum?), null)]
        [InlineData(1L, typeof(LongEnum?), LongEnum.Value1)]
        [InlineData(null, typeof(LongEnum?), null)]
        [InlineData(uint.MaxValue, typeof(UnsignedIntEnum?), UnsignedIntEnum.MaxValue)]
        [InlineData((byte)1, typeof(ByteEnum?), ByteEnum.Value1)]
        [InlineData(null, typeof(ByteEnum?), null)]
        [InlineData((ushort)1, typeof(LongEnum?), LongEnum.Value1)]
        public void ConvertToReturnsValueIfArrayElementIsAnyIntegerTypeAndDestinationTypeIsEnum(
            object input,
            Type enumType,
            object expected)
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { input }, enumType, culture: null);

            // Assert
            Assert.Equal(expected, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsStringValueAndDestinationTypeIsEnum()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { "1" }, typeof(IntEnum), culture: null);

            // Assert
            Assert.Equal(IntEnum.Value1, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsStringKeyAndDestinationTypeIsEnum()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { "Value1" }, typeof(IntEnum), culture: null);

            // Assert
            Assert.Equal(IntEnum.Value1, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsStringAndDestinationIsNullableInteger()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo("12", typeof(int?), culture: null);

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsStringAndDestinationIsNullableDouble()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo("12.5", typeof(double?), culture: null);

            // Assert
            Assert.Equal(12.5, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalAndDestinationIsNullableInteger()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(12M, typeof(int?), culture: null);

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalAndDestinationIsNullableDouble()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(12.5M, typeof(double?), culture: null);

            // Assert
            Assert.Equal(12.5, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalDoubleAndDestinationIsNullableInteger()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(12M, typeof(int?), culture: null);

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalDoubleAndDestinationIsNullableLong()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(12M, typeof(long?), culture: null);

            // Assert
            Assert.Equal(12L, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementInstanceOfDestinationType()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { "some string" }, typeof(string), culture: null);

            // Assert
            Assert.Equal("some string", outValue);
        }

        [Theory]
        [InlineData(new object[] { new object[] { 1, 0 } })]
        [InlineData(new object[] { new[] { "Value1", "Value0" } })]
        [InlineData(new object[] { new[] { "Value1", "value0" } })]
        public void ConvertTo_ConvertsEnumArrays(object value)
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(value, typeof(IntEnum[]), culture: null);

            // Assert
            var result = Assert.IsType<IntEnum[]>(outValue);
            Assert.Equal(2, result.Length);
            Assert.Equal(IntEnum.Value1, result[0]);
            Assert.Equal(IntEnum.Value0, result[1]);
        }

        [Theory]
        [InlineData(new object[] { new object[] { 1, 2 }, new[] { FlagsEnum.Value1, FlagsEnum.Value2 } })]
        [InlineData(new object[] { new[] { "Value1", "Value2" }, new[] { FlagsEnum.Value1, FlagsEnum.Value2 } })]
        [InlineData(new object[] { new object[] { 5, 2 }, new[] { FlagsEnum.Value1 | FlagsEnum.Value4, FlagsEnum.Value2 } })]
        public void ConvertTo_ConvertsFlagsEnumArrays(object value, FlagsEnum[] expected)
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(value, typeof(FlagsEnum[]), culture: null);

            // Assert
            var result = Assert.IsType<FlagsEnum[]>(outValue);
            Assert.Equal(2, result.Length);
            Assert.Equal(expected[0], result[0]);
            Assert.Equal(expected[1], result[1]);
        }

        [Fact]
        public void ConvertToReturnsValueIfInstanceOfDestinationType()
        {
            // Arrange
            var original = new[] { "some string" };

            // Act
            var outValue = ModelBindingHelper.ConvertTo(original, typeof(string[]), culture: null);

            // Assert
            Assert.Same(original, outValue);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(double?))]
        [InlineData(typeof(IntEnum?))]
        public void ConvertToThrowsIfConverterThrows(Type destinationType)
        {
            // Arrange

            // Act & Assert
            var ex = Assert.Throws<FormatException>(
                () => ModelBindingHelper.ConvertTo("this-is-not-a-valid-value", destinationType, culture: null));
        }

        [Fact]
        public void ConvertToUsesProvidedCulture()
        {
            // Arrange

            // Act
            var cultureResult = ModelBindingHelper.ConvertTo("12,5", typeof(decimal), new CultureInfo("fr-FR"));

            // Assert
            Assert.Equal(12.5M, cultureResult);
            Assert.Throws<FormatException>(
                () => ModelBindingHelper.ConvertTo("12,5", typeof(decimal), new CultureInfo("en-GB")));
        }

        [Theory]
        [MemberData(nameof(IntrinsicConversionData))]
        public void ConvertToCanConvertIntrinsics<T>(object initialValue, T expectedValue)
        {
            // Arrange

            // Act & Assert
            Assert.Equal(expectedValue, ModelBindingHelper.ConvertTo(initialValue, typeof(T), culture: null));
        }

        public static IEnumerable<object[]> IntrinsicConversionData
        {
            get
            {
                yield return new object[] { 42, 42L };
                yield return new object[] { 42, (short)42 };
                yield return new object[] { 42, (float)42.0 };
                yield return new object[] { 42, (double)42.0 };
                yield return new object[] { 42M, 42 };
                yield return new object[] { 42L, 42 };
                yield return new object[] { 42, (byte)42 };
                yield return new object[] { (short)42, 42 };
                yield return new object[] { (float)42.0, 42 };
                yield return new object[] { (double)42.0, 42 };
                yield return new object[] { (byte)42, 42 };
                yield return new object[] { "2008-01-01", new DateTime(2008, 01, 01) };
                yield return new object[] { "00:00:20", TimeSpan.FromSeconds(20) };
                yield return new object[]
                {
                    "c6687d3a-51f9-4159-8771-a66d2b7d7038",
                    Guid.Parse("c6687d3a-51f9-4159-8771-a66d2b7d7038")
                };
            }
        }

        // None of the types here have converters from MyClassWithoutConverter.
        [Theory]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(IntEnum))]
        public void ConvertTo_Throws_IfValueIsNotConvertible(Type destinationType)
        {
            // Arrange
            var expectedMessage = $"The parameter conversion from type '{typeof(MyClassWithoutConverter)}' to type " +
                $"'{destinationType}' failed because no type converter can convert between these types.";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => ModelBindingHelper.ConvertTo(new MyClassWithoutConverter(), destinationType, culture: null));
            Assert.Equal(expectedMessage, ex.Message);
        }

        // String does not have a converter to MyClassWithoutConverter.
        [Fact]
        public void ConvertTo_Throws_IfDestinationTypeIsNotConvertible()
        {
            // Arrange
            var value = "Hello world";
            var destinationType = typeof(MyClassWithoutConverter);
            var expectedMessage = $"The parameter conversion from type '{value.GetType()}' to type " +
                $"'{typeof(MyClassWithoutConverter)}' failed because no type converter can convert between these types.";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => ModelBindingHelper.ConvertTo(value, destinationType, culture: null));
            Assert.Equal(expectedMessage, ex.Message);
        }

        // Happens very rarely in practice since conversion is almost-always from strings or string arrays.
        [Theory]
        [InlineData(typeof(MyClassWithoutConverter))]
        [InlineData(typeof(MySubClassWithoutConverter))]
        public void ConvertTo_ReturnsValue_IfCompatible(Type destinationType)
        {
            // Arrange
            var value = new MySubClassWithoutConverter();

            // Act
            var result = ModelBindingHelper.ConvertTo(value, destinationType, culture: null);

            // Assert
            Assert.Same(value, result);
        }

        [Theory]
        [InlineData(typeof(MyClassWithoutConverter[]))]
        [InlineData(typeof(MySubClassWithoutConverter[]))]
        public void ConvertTo_ReusesArrayElements_IfCompatible(Type destinationType)
        {
            // Arrange
            var value = new MyClassWithoutConverter[]
            {
                new MySubClassWithoutConverter(),
                new MySubClassWithoutConverter(),
                new MySubClassWithoutConverter(),
            };

            // Act
            var result = ModelBindingHelper.ConvertTo(value, destinationType, culture: null);

            // Assert
            Assert.IsType(destinationType, result);
            Assert.Collection(
                result as IEnumerable<MyClassWithoutConverter>,
                element => { Assert.Same(value[0], element); },
                element => { Assert.Same(value[1], element); },
                element => { Assert.Same(value[2], element); });
        }

        [Theory]
        [InlineData(new object[] { 2, FlagsEnum.Value2 })]
        [InlineData(new object[] { 5, FlagsEnum.Value1 | FlagsEnum.Value4 })]
        [InlineData(new object[] { 15, FlagsEnum.Value1 | FlagsEnum.Value2 | FlagsEnum.Value4 | FlagsEnum.Value8 })]
        [InlineData(new object[] { 16, (FlagsEnum)16 })]
        [InlineData(new object[] { 0, (FlagsEnum)0 })]
        [InlineData(new object[] { null, (FlagsEnum)0 })]
        [InlineData(new object[] { "Value1,Value2", (FlagsEnum)3 })]
        [InlineData(new object[] { "Value1,Value2,value4, value8", (FlagsEnum)15 })]
        public void ConvertTo_ConvertsEnumFlags(object value, object expected)
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo<FlagsEnum>(value, culture: null);

            // Assert
            Assert.Equal(expected, outValue);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(IEnumerable<int>))]
        [InlineData(typeof(IReadOnlyCollection<int>))]
        [InlineData(typeof(IReadOnlyList<int>))]
        [InlineData(typeof(ICollection<int>))]
        [InlineData(typeof(IList<int>))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(Collection<int>))]
        [InlineData(typeof(IntList))]
        [InlineData(typeof(LinkedList<int>))]
        public void CanGetCompatibleCollection_ReturnsTrue(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);

            // Act
            var result = ModelBindingHelper.CanGetCompatibleCollection<int>(bindingContext);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(IEnumerable<int>))]
        [InlineData(typeof(IReadOnlyCollection<int>))]
        [InlineData(typeof(IReadOnlyList<int>))]
        [InlineData(typeof(ICollection<int>))]
        [InlineData(typeof(IList<int>))]
        [InlineData(typeof(List<int>))]
        public void GetCompatibleCollection_ReturnsList(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);

            // Act
            var result = ModelBindingHelper.GetCompatibleCollection<int>(bindingContext);

            // Assert
            Assert.IsType<List<int>>(result);
        }

        [Theory]
        [InlineData(typeof(Collection<int>))]
        [InlineData(typeof(IntList))]
        [InlineData(typeof(LinkedList<int>))]
        public void GetCompatibleCollection_ActivatesCollection(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);

            // Act
            var result = ModelBindingHelper.GetCompatibleCollection<int>(bindingContext);

            // Assert
            Assert.IsType(destinationType, result);
        }

        [Fact]
        public void GetCompatibleCollection_SetsCapacity()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(IList<int>));

            // Act
            var result = ModelBindingHelper.GetCompatibleCollection<int>(bindingContext, capacity: 23);

            // Assert
            var list = Assert.IsType<List<int>>(result);
            Assert.Equal(23, list.Capacity);
        }

        [Theory]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.ArrayProperty))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.ArrayPropertyWithValue))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.EnumerableProperty))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.EnumerablePropertyWithArrayValue))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.ListProperty))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.ScalarProperty))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.ScalarPropertyWithValue))]
        public void CanGetCompatibleCollection_ReturnsTrue_IfReadOnly(string propertyName)
        {
            // Arrange
            var bindingContext = GetBindingContextForProperty(propertyName);

            // Act
            var result = ModelBindingHelper.CanGetCompatibleCollection<int>(bindingContext);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.EnumerablePropertyWithArrayValueAndSetter))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.EnumerablePropertyWithListValue))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.ListPropertyWithValue))]
        public void CanGetCompatibleCollection_ReturnsTrue_IfCollection(string propertyName)
        {
            // Arrange
            var bindingContext = GetBindingContextForProperty(propertyName);

            // Act
            var result = ModelBindingHelper.CanGetCompatibleCollection<int>(bindingContext);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.EnumerablePropertyWithListValue))]
        [InlineData(nameof(ModelWithReadOnlyAndSpecialCaseProperties.ListPropertyWithValue))]
        public void GetCompatibleCollection_ReturnsExistingCollection(string propertyName)
        {
            // Arrange
            var bindingContext = GetBindingContextForProperty(propertyName);

            // Act
            var result = ModelBindingHelper.GetCompatibleCollection<int>(bindingContext);

            // Assert
            Assert.Same(bindingContext.Model, result);
            var list = Assert.IsType<List<int>>(result);
            Assert.Empty(list);
        }

        [Fact]
        public void CanGetCompatibleCollection_ReturnsNewCollection()
        {
            // Arrange
            var bindingContext = GetBindingContextForProperty(
                nameof(ModelWithReadOnlyAndSpecialCaseProperties.EnumerablePropertyWithArrayValueAndSetter));

            // Act
            var result = ModelBindingHelper.GetCompatibleCollection<int>(bindingContext);

            // Assert
            Assert.NotSame(bindingContext.Model, result);
            var list = Assert.IsType<List<int>>(result);
            Assert.Empty(list);
        }

        [Theory]
        [InlineData(typeof(Collection<string>))]
        [InlineData(typeof(List<long>))]
        [InlineData(typeof(MyModel))]
        [InlineData(typeof(AbstractIntList))]
        [InlineData(typeof(ISet<int>))]
        public void CanGetCompatibleCollection_ReturnsFalse(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);

            // Act
            var result = ModelBindingHelper.CanGetCompatibleCollection<int>(bindingContext);

            // Assert
            Assert.False(result);
        }

        private static DefaultModelBindingContext GetBindingContextForProperty(string propertyName)
        {
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithReadOnlyAndSpecialCaseProperties),
                propertyName);
            var bindingContext = GetBindingContext(modelMetadata);

            var container = new ModelWithReadOnlyAndSpecialCaseProperties();
            bindingContext.Model = modelMetadata.PropertyGetter(container);

            return bindingContext;
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(modelType);

            return GetBindingContext(metadata);
        }

        private static DefaultModelBindingContext GetBindingContext(ModelMetadata metadata)
        {
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = metadata,
            };

            return bindingContext;
        }

        private class ModelWithReadOnlyAndSpecialCaseProperties
        {
            public int[] ArrayProperty { get; }

            public int[] ArrayPropertyWithValue { get; } = new int[4];

            public IEnumerable<int> EnumerableProperty { get; }

            public IEnumerable<int> EnumerablePropertyWithArrayValue { get; } = new int[4];

            // Special case: Value cannot be used but property can be set.
            public IEnumerable<int> EnumerablePropertyWithArrayValueAndSetter { get; set; } = new int[4];

            public IEnumerable<int> EnumerablePropertyWithListValue { get; } = new List<int> { 23 };

            public List<int> ListProperty { get; }

            public List<int> ListPropertyWithValue { get; } = new List<int> { 23 };

            public int ScalarProperty { get; }

            public int ScalarPropertyWithValue { get; } = 23;
        }

        private class MyClassWithoutConverter
        {
        }

        private class MySubClassWithoutConverter : MyClassWithoutConverter
        {
        }

        private abstract class AbstractIntList : List<int>
        {
        }

        private class IntList : List<int>
        {
        }

        private enum IntEnum
        {
            Value0 = 0,
            Value1 = 1,
            MaxValue = int.MaxValue
        }

        private enum LongEnum : long
        {
            Value0 = 0L,
            Value1 = 1L,
            MaxValue = long.MaxValue
        }

        private enum UnsignedIntEnum : uint
        {
            Value0 = 0U,
            Value1 = 1U,
            MaxValue = uint.MaxValue
        }

        private enum ByteEnum : byte
        {
            Value0 = 0,
            Value1 = 1,
            MaxValue = byte.MaxValue
        }

        [Flags]
        public enum FlagsEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value4 = 4,
            Value8 = 8
        }
    }
}
