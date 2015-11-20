// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.DataAnnotations;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelBindingHelperTest
    {
        public static TheoryData<ModelBindingResult> UnsuccessfulModelBindingData
        {
            get
            {
                return new TheoryData<ModelBindingResult>
                {
                    ModelBindingResult.NoResult,
                    ModelBindingResult.Failed("someKey"),
                };
            }
        }

        [Theory]
        [MemberData(nameof(UnsuccessfulModelBindingData))]
        public async Task TryUpdateModel_ReturnsFalse_IfBinderIsUnsuccessful(ModelBindingResult binderResult)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(binderResult));
            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                string.Empty,
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetCompositeBinder(binder.Object),
                Mock.Of<IValueProvider>(),
                new List<IInputFormatter>(),
                new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                Mock.Of<IModelValidatorProvider>());

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
        }

        [Fact]
        public async Task TryUpdateModel_ReturnsFalse_IfModelValidationFails()
        {
            // Arrange
            // Mono issue - https://github.com/aspnet/External/issues/19
            var expectedMessage = PlatformNormalizer.NormalizeContent("The MyProperty field is required.");
            var binders = new IModelBinder[]
            {
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
                stringLocalizerFactory: null);
            var model = new MyModel();

            var values = new Dictionary<string, object>
            {
                { "", null }
            };
            var valueProvider = new TestValueProvider(values);
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            var actionContext = new ActionContext() { HttpContext = new DefaultHttpContext() };
            var modelState = actionContext.ModelState;

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                "",
                actionContext,
                modelMetadataProvider,
                GetCompositeBinder(binders),
                valueProvider,
                new List<IInputFormatter>(),
                new DefaultObjectValidator(modelMetadataProvider),
                validator);

            // Assert
            Assert.False(result);
            var error = Assert.Single(modelState["MyProperty"].Errors);
            Assert.Equal(expectedMessage, error.ErrorMessage);
        }

        [Fact]
        public async Task TryUpdateModel_ReturnsTrue_IfModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
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
                GetCompositeBinder(binders),
                valueProvider,
                new List<IInputFormatter>(),
                new DefaultObjectValidator(metadataProvider),
                validator);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
        }

        [Theory]
        [MemberData(nameof(UnsuccessfulModelBindingData))]
        public async Task TryUpdateModel_UsingIncludePredicateOverload_ReturnsFalse_IfBinderIsUnsuccessful(
            ModelBindingResult binderResult)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(binderResult));
            var model = new MyModel();
            Func<ModelBindingContext, string, bool> includePredicate = (context, propertyName) => true;

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                string.Empty,
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetCompositeBinder(binder.Object),
                Mock.Of<IValueProvider>(),
                new List<IInputFormatter>(),
                new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                Mock.Of<IModelValidatorProvider>(),
                includePredicate);

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            Assert.Null(model.IncludedProperty);
            Assert.Null(model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModel_UsingIncludePredicateOverload_ReturnsTrue_ModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
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

            Func<ModelBindingContext, string, bool> includePredicate = (context, propertyName) =>
                string.Equals(propertyName, "IncludedProperty", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(propertyName, "MyProperty", StringComparison.OrdinalIgnoreCase);

            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                "",
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetCompositeBinder(binders),
                valueProvider,
                new List<IInputFormatter>(),
                new DefaultObjectValidator(metadataProvider),
                validator,
                includePredicate);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
            Assert.Equal("IncludedPropertyValue", model.IncludedProperty);
            Assert.Equal("Old-ExcludedPropertyValue", model.ExcludedProperty);
        }

        [Theory]
        [MemberData(nameof(UnsuccessfulModelBindingData))]
        public async Task TryUpdateModel_UsingIncludeExpressionOverload_ReturnsFalse_IfBinderIsUnsuccessful(
            ModelBindingResult binderResult)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(binderResult));
            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                string.Empty,
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetCompositeBinder(binder.Object),
                Mock.Of<IValueProvider>(),
                new List<IInputFormatter>(),
                new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                Mock.Of<IModelValidatorProvider>(),
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
            var binders = new IModelBinder[]
            {
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
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
                GetCompositeBinder(binders),
                valueProvider,
                new List<IInputFormatter>(),
                new DefaultObjectValidator(metadataProvider),
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
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
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
                GetCompositeBinder(binders),
                valueProvider,
                new List<IInputFormatter>(),
                new DefaultObjectValidator(metadataProvider),
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

        [Theory]
        [MemberData(nameof(UnsuccessfulModelBindingData))]
        public async Task TryUpdateModelNonGeneric_PredicateOverload_ReturnsFalse_IfBinderIsUnsuccessful(
            ModelBindingResult binderResult)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(binderResult));
            var model = new MyModel();
            Func<ModelBindingContext, string, bool> includePredicate = (context, propertyName) => true;

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                model.GetType(),
                prefix: "",
                actionContext: new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider: metadataProvider,
                modelBinder: GetCompositeBinder(binder.Object),
                valueProvider: Mock.Of<IValueProvider>(),
                inputFormatters: new List<IInputFormatter>(),
                objectModelValidator: new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                validatorProvider: Mock.Of<IModelValidatorProvider>(),
                predicate: includePredicate);

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
            Assert.Null(model.IncludedProperty);
            Assert.Null(model.ExcludedProperty);
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_PredicateOverload_ReturnsTrue_ModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
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

            Func<ModelBindingContext, string, bool> includePredicate =
                (context, propertyName) =>
                                string.Equals(propertyName, "IncludedProperty", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(propertyName, "MyProperty", StringComparison.OrdinalIgnoreCase);

            var valueProvider = new TestValueProvider(values);
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                model.GetType(),
                "",
                new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider,
                GetCompositeBinder(binders),
                valueProvider,
                new List<IInputFormatter>(),
                new DefaultObjectValidator(metadataProvider),
                validator,
                includePredicate);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
            Assert.Equal("IncludedPropertyValue", model.IncludedProperty);
            Assert.Equal("Old-ExcludedPropertyValue", model.ExcludedProperty);
        }

        [Theory]
        [MemberData(nameof(UnsuccessfulModelBindingData))]
        public async Task TryUpdateModelNonGeneric_ModelTypeOverload_ReturnsFalse_IfBinderIsUnsuccessful(
            ModelBindingResult binderResult)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(binderResult));
            var model = new MyModel();

            // Act
            var result = await ModelBindingHelper.TryUpdateModelAsync(
                model,
                modelType: model.GetType(),
                prefix: "",
                actionContext: new ActionContext() { HttpContext = new DefaultHttpContext() },
                metadataProvider: metadataProvider,
                modelBinder: GetCompositeBinder(binder.Object),
                valueProvider: Mock.Of<IValueProvider>(),
                inputFormatters: new List<IInputFormatter>(),
                objectModelValidator: new Mock<IObjectModelValidator>(MockBehavior.Strict).Object,
                validatorProvider: Mock.Of<IModelValidatorProvider>());

            // Assert
            Assert.False(result);
            Assert.Null(model.MyProperty);
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_ModelTypeOverload_ReturnsTrue_IfModelBindsAndValidatesSuccessfully()
        {
            // Arrange
            var binders = new IModelBinder[]
            {
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder()
            };

            var validator = new DataAnnotationsModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
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
                TestModelMetadataProvider.CreateDefaultProvider(),
                GetCompositeBinder(binders),
                valueProvider,
                new List<IInputFormatter>(),
                new DefaultObjectValidator(metadataProvider),
                validator);

            // Assert
            Assert.True(result);
            Assert.Equal("MyPropertyValue", model.MyProperty);
        }

        [Fact]
        public async Task TryUpdataModel_ModelTypeDifferentFromModel_Throws()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();

            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Returns(ModelBindingResult.NoResultAsync);
            var model = new MyModel();
            Func<ModelBindingContext, string, bool> includePredicate =
               (context, propertyName) => true;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => ModelBindingHelper.TryUpdateModelAsync(
                    model,
                    typeof(User),
                    "",
                    new ActionContext() { HttpContext = new DefaultHttpContext() },
                    metadataProvider,
                    GetCompositeBinder(binder.Object),
                    Mock.Of<IValueProvider>(),
                    new List<IInputFormatter>(),
                    new DefaultObjectValidator(metadataProvider),
                    Mock.Of<IModelValidatorProvider>(),
                    includePredicate));

            var expectedMessage = string.Format("The model's runtime type '{0}' is not assignable to the type '{1}'." +
                Environment.NewLine +
                "Parameter name: modelType",
                model.GetType().FullName,
                typeof(User).FullName);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ClearValidationStateForModel_EmtpyModelKey(string modelKey)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var dictionary = new ModelStateDictionary();
            dictionary["Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Name", "MyProperty invalid.");
            dictionary["Id"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("Id", "Id invalid.");
            dictionary.AddModelError("Id", "Id is required.");
            dictionary["Category"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };

            // Act
            ModelBindingHelper.ClearValidationStateForModel(
                typeof(Product),
                dictionary,
                metadataProvider,
                modelKey);

            // Assert
            Assert.Equal(0, dictionary["Name"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Name"].ValidationState);
            Assert.Equal(0, dictionary["Id"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Id"].ValidationState);
            Assert.Equal(0, dictionary["Category"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["Category"].ValidationState);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ClearValidationStateForCollectionsModel_EmtpyModelKey(string modelKey)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var dictionary = new ModelStateDictionary();
            dictionary["[0].Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("[0].Name", "Name invalid.");
            dictionary["[0].Id"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("[0].Id", "Id invalid.");
            dictionary.AddModelError("[0].Id", "Id required.");
            dictionary["[0].Category"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };

            dictionary["[1].Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary["[1].Id"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary["[1].Category"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("[1].Category", "Category invalid.");

            // Act
            ModelBindingHelper.ClearValidationStateForModel(
                typeof(List<Product>),
                dictionary,
                metadataProvider,
                modelKey);

            // Assert
            Assert.Equal(0, dictionary["[0].Name"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[0].Name"].ValidationState);
            Assert.Equal(0, dictionary["[0].Id"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[0].Id"].ValidationState);
            Assert.Equal(0, dictionary["[0].Category"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[0].Category"].ValidationState);
            Assert.Equal(0, dictionary["[1].Name"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[1].Name"].ValidationState);
            Assert.Equal(0, dictionary["[1].Id"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[1].Id"].ValidationState);
            Assert.Equal(0, dictionary["[1].Category"].Errors.Count);
            Assert.Equal(ModelValidationState.Unvalidated, dictionary["[1].Category"].ValidationState);
        }

        [Theory]
        [InlineData("product")]
        [InlineData("product.Name")]
        [InlineData("product.Order[0].Name")]
        [InlineData("product.Order[0].Address.Street")]
        [InlineData("product.Category.Name")]
        [InlineData("product.Order")]
        public void ClearValidationStateForModel_NonEmtpyModelKey(string prefix)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var dictionary = new ModelStateDictionary();
            dictionary["product.Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("product.Name", "Name invalid.");
            dictionary["product.Id"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("product.Id", "Id invalid.");
            dictionary.AddModelError("product.Id", "Id required.");
            dictionary["product.Category"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary["product.Category.Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary["product.Order[0].Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("product.Order[0].Name", "Order name invalid.");
            dictionary["product.Order[0].Address.Street"] =
                new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("product.Order[0].Address.Street", "Street invalid.");
            dictionary["product.Order[1].Name"] = new ModelStateEntry { ValidationState = ModelValidationState.Valid };
            dictionary["product.Order[0]"] = new ModelStateEntry { ValidationState = ModelValidationState.Invalid };
            dictionary.AddModelError("product.Order[0]", "Order invalid.");

            // Act
            ModelBindingHelper.ClearValidationStateForModel(
                typeof(Product),
                dictionary,
                metadataProvider,
                prefix);

            // Assert
            foreach (var entry in dictionary.Keys)
            {
                if (entry.StartsWith(prefix))
                {
                    Assert.Equal(0, dictionary[entry].Errors.Count);
                    Assert.Equal(ModelValidationState.Unvalidated, dictionary[entry].ValidationState);
                }
            }
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
            var convertedValue = ModelBindingHelper.ConvertTo(null, typeof(string));
            Assert.Null(convertedValue);
        }

        [Fact]
        public void ConvertTo_ReturnsDefaultForValueTypes_WhenValueIsNull()
        {
            var convertedValue = ModelBindingHelper.ConvertTo(null, typeof(int));
            Assert.Equal(0, convertedValue);
        }

        [Fact]
        public void ConvertToCanConvertArraysToSingleElements()
        {
            // Arrange
            var value = new int[] { 1, 20, 42 };

            // Act
            var converted = ModelBindingHelper.ConvertTo(value, typeof(string));

            // Assert
            Assert.Equal("1", converted);
        }

        [Fact]
        public void ConvertToCanConvertSingleElementsToArrays()
        {
            // Arrange
            var value = 42;

            // Act
            var converted = ModelBindingHelper.ConvertTo<string[]>(value);

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
            var converted = ModelBindingHelper.ConvertTo<string>(42);

            // Assert
            Assert.NotNull(converted);
            Assert.Equal("42", converted);
        }

        [Fact]
        public void ConvertingNullStringToNullableIntReturnsNull()
        {
            // Arrange

            // Act
            var returned = ModelBindingHelper.ConvertTo<int?>(null);

            // Assert
            Assert.Equal(returned, null);
        }

        [Fact]
        public void ConvertingWhiteSpaceStringToNullableIntReturnsNull()
        {
            // Arrange
            var original = " ";

            // Act
            var returned = ModelBindingHelper.ConvertTo<int?>(original);

            // Assert
            Assert.Equal(returned, null);
        }

        [Fact]
        public void ConvertToReturnsNullIfArrayElementValueIsNull()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new string[] { null }, typeof(int));

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsNullIfTryingToConvertEmptyArrayToSingleElement()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new int[0], typeof(int));

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
            var outValue = ModelBindingHelper.ConvertTo(value, typeof(int));

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsNullIfTrimmedValueIsEmptyString()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(null, typeof(int[]));

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsIntegerAndDestinationTypeIsEnum()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { 1 }, typeof(IntEnum));

            // Assert
            Assert.Equal(outValue, IntEnum.Value1);
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
            var outValue = ModelBindingHelper.ConvertTo(new object[] { input }, enumType);

            // Assert
            Assert.Equal(expected, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsStringValueAndDestinationTypeIsEnum()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { "1" }, typeof(IntEnum));

            // Assert
            Assert.Equal(outValue, IntEnum.Value1);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsStringKeyAndDestinationTypeIsEnum()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { "Value1" }, typeof(IntEnum));

            // Assert
            Assert.Equal(outValue, IntEnum.Value1);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsStringAndDestinationIsNullableInteger()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo("12", typeof(int?));

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsStringAndDestinationIsNullableDouble()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo("12.5", typeof(double?));

            // Assert
            Assert.Equal(12.5, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalAndDestinationIsNullableInteger()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(12M, typeof(int?));

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalAndDestinationIsNullableDouble()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(12.5M, typeof(double?));

            // Assert
            Assert.Equal(12.5, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalDoubleAndDestinationIsNullableInteger()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(12M, typeof(int?));

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalDoubleAndDestinationIsNullableLong()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(12M, typeof(long?));

            // Assert
            Assert.Equal(12L, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementInstanceOfDestinationType()
        {
            // Arrange

            // Act
            var outValue = ModelBindingHelper.ConvertTo(new object[] { "some string" }, typeof(string));

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
            var outValue = ModelBindingHelper.ConvertTo(value, typeof(IntEnum[]));

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
            var outValue = ModelBindingHelper.ConvertTo(value, typeof(FlagsEnum[]));

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
            var outValue = ModelBindingHelper.ConvertTo(original, typeof(string[]));

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
                () => ModelBindingHelper.ConvertTo("this-is-not-a-valid-value", destinationType));
        }

        [Fact]
        public void ConvertToThrowsIfNoConverterExists()
        {
            // Arrange
            var destinationType = typeof(MyClassWithoutConverter);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => ModelBindingHelper.ConvertTo("x", destinationType));
            Assert.Equal("The parameter conversion from type 'System.String' to type " +
                        $"'{typeof(MyClassWithoutConverter).FullName}' " +
                        "failed because no type converter can convert between these types.",
                         ex.Message);
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
            Assert.Equal(expectedValue, ModelBindingHelper.ConvertTo(initialValue, typeof(T)));
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

        [Theory]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(IntEnum))]
        public void ConvertTo_Throws_IfValueIsNotStringData(Type destinationType)
        {
            // Arrange

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => ModelBindingHelper.ConvertTo(new MyClassWithoutConverter(), destinationType));

            // Assert
            var expectedMessage = string.Format("The parameter conversion from type '{0}' to type '{1}' " +
                                                "failed because no type converter can convert between these types.",
                                                typeof(MyClassWithoutConverter), destinationType);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ConvertTo_Throws_IfDestinationTypeIsNotConvertible()
        {
            // Arrange
            var value = "Hello world";
            var destinationType = typeof(MyClassWithoutConverter);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => ModelBindingHelper.ConvertTo(value, destinationType));

            // Assert
            var expectedMessage = string.Format("The parameter conversion from type '{0}' to type '{1}' " +
                                                "failed because no type converter can convert between these types.",
                                                value.GetType(), typeof(MyClassWithoutConverter));
            Assert.Equal(expectedMessage, ex.Message);
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
            var outValue = ModelBindingHelper.ConvertTo<FlagsEnum>(value);

            // Assert
            Assert.Equal(expected, outValue);
        }

        private class MyClassWithoutConverter
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
