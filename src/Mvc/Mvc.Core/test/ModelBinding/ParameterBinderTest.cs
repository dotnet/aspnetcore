// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ParameterBinderTest
{
    private static readonly IOptions<MvcOptions> _optionsAccessor = Options.Create(new MvcOptions());

    public static TheoryData BindModelAsyncData
    {
        get
        {
            var emptyBindingInfo = new BindingInfo();
            var bindingInfoWithName = new BindingInfo
            {
                BinderModelName = "bindingInfoName",
                BinderType = typeof(SimpleTypeModelBinder),
            };

            // parameterBindingInfo, metadataBinderModelName, parameterName, expectedBinderModelName
            return new TheoryData<BindingInfo, string, string, string>
                {
                    // If the parameter name is not a prefix match, it is ignored. But name is required to create a
                    // ModelBindingContext.
                    { null, null, "parameterName", string.Empty },
                    { emptyBindingInfo, null, "parameterName", string.Empty },
                    { bindingInfoWithName, null, "parameterName", "bindingInfoName" },
                    { null, "modelBinderName", "parameterName", "modelBinderName" },
                    { null, null, "parameterName", string.Empty },
                    // Parameter's BindingInfo has highest precedence
                    { bindingInfoWithName, "modelBinderName", "parameterName", "bindingInfoName" },
                };
        }
    }

    [Fact]
    public async Task BindModelAsync_EnforcesTopLevelBindRequired()
    {
        // Arrange
        var actionContext = GetControllerContext();

        var mockModelMetadata = CreateMockModelMetadata();
        mockModelMetadata.Setup(o => o.IsBindingRequired).Returns(true);
        mockModelMetadata.Setup(o => o.DisplayName).Returns("Ignored Display Name"); // Bind attribute errors are phrased in terms of the model name, not display name

        var parameterBinder = CreateParameterBinder(mockModelMetadata.Object);
        var modelBindingResult = ModelBindingResult.Failed();

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            CreateMockModelBinder(modelBindingResult),
            CreateMockValueProvider(),
            new ParameterDescriptor { Name = "myParam", ParameterType = typeof(Person) },
            mockModelMetadata.Object,
            "ignoredvalue");

        // Assert
        Assert.False(actionContext.ModelState.IsValid);
        Assert.Equal("myParam", actionContext.ModelState.Single().Key);
        Assert.Equal(
            new DefaultModelBindingMessageProvider().MissingBindRequiredValueAccessor("myParam"),
            actionContext.ModelState.Single().Value.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task BindModelAsync_EnforcesTopLevelRequired()
    {
        // Arrange
        var actionContext = GetControllerContext();
        var mockModelMetadata = CreateMockModelMetadata();
        mockModelMetadata.Setup(o => o.IsRequired).Returns(true);
        mockModelMetadata.Setup(o => o.DisplayName).Returns("My Display Name");
        mockModelMetadata.Setup(o => o.ValidatorMetadata).Returns(new[]
        {
                new RequiredAttribute()
            });

        var validator = new DataAnnotationsModelValidator(
            new ValidationAttributeAdapterProvider(),
            new RequiredAttribute(),
            stringLocalizer: null);

        var parameterBinder = CreateParameterBinder(mockModelMetadata.Object, validator);
        var modelBindingResult = ModelBindingResult.Success(null);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            CreateMockModelBinder(modelBindingResult),
            CreateMockValueProvider(),
            new ParameterDescriptor { Name = "myParam", ParameterType = typeof(Person) },
            mockModelMetadata.Object,
            "ignoredvalue");

        // Assert
        Assert.False(actionContext.ModelState.IsValid);
        Assert.Equal("myParam", actionContext.ModelState.Single().Key);
        Assert.Equal(
            new RequiredAttribute().FormatErrorMessage("My Display Name"),
            actionContext.ModelState.Single().Value.Errors.Single().ErrorMessage);
    }

    public static TheoryData<RequiredAttribute, ParameterDescriptor, ModelMetadata> EnforcesTopLevelRequiredDataSet
    {
        get
        {
            var attribute = new RequiredAttribute();
            var bindingInfo = new BindingInfo
            {
                BinderModelName = string.Empty,
            };
            var parameterDescriptor = new ParameterDescriptor
            {
                Name = string.Empty,
                BindingInfo = bindingInfo,
                ParameterType = typeof(Person),
            };

            var method = typeof(Person).GetMethod(nameof(Person.Equals), new[] { typeof(Person) });
            var parameter = method.GetParameters()[0]; // Equals(Person other)
            var controllerParameterDescriptor = new ControllerParameterDescriptor
            {
                Name = string.Empty,
                BindingInfo = bindingInfo,
                ParameterInfo = parameter,
                ParameterType = typeof(Person),
            };

            var provider1 = new TestModelMetadataProvider();
            provider1
                .ForParameter(parameter)
                .ValidationDetails(d =>
                {
                    d.IsRequired = true;
                    d.ValidatorMetadata.Add(attribute);
                });
            provider1
                .ForProperty(typeof(Family), nameof(Family.Mom))
                .ValidationDetails(d =>
                {
                    d.IsRequired = true;
                    d.ValidatorMetadata.Add(attribute);
                });

            var provider2 = new TestModelMetadataProvider();
            provider2
                .ForType(typeof(Person))
                .ValidationDetails(d =>
                {
                    d.IsRequired = true;
                    d.ValidatorMetadata.Add(attribute);
                });

            return new TheoryData<RequiredAttribute, ParameterDescriptor, ModelMetadata>
                {
                    { attribute, parameterDescriptor, provider1.GetMetadataForParameter(parameter) },
                    { attribute, parameterDescriptor, provider1.GetMetadataForProperty(typeof(Family), nameof(Family.Mom)) },
                    { attribute, parameterDescriptor, provider2.GetMetadataForType(typeof(Person)) },
                    { attribute, controllerParameterDescriptor, provider2.GetMetadataForType(typeof(Person)) },
                };
        }
    }

    [Theory]
    [MemberData(nameof(EnforcesTopLevelRequiredDataSet))]
    public async Task BindModelAsync_EnforcesTopLevelRequiredAndLogsSuccessfully_WithEmptyPrefix(
        RequiredAttribute attribute,
        ParameterDescriptor parameterDescriptor,
        ModelMetadata metadata)
    {
        // Arrange
        var expectedKey = string.Empty;
        var expectedFieldName = metadata.Name ?? nameof(Person);

        var actionContext = GetControllerContext();
        var validator = new DataAnnotationsModelValidator(
            new ValidationAttributeAdapterProvider(),
            attribute,
            stringLocalizer: null);

        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var parameterBinder = CreateParameterBinder(metadata, validator, loggerFactory: loggerFactory);
        var modelBindingResult = ModelBindingResult.Success(null);

        // Act
        await parameterBinder.BindModelAsync(
            actionContext,
            CreateMockModelBinder(modelBindingResult),
            CreateMockValueProvider(),
            parameterDescriptor,
            metadata,
            "ignoredvalue");

        // Assert
        Assert.False(actionContext.ModelState.IsValid);
        var modelState = Assert.Single(actionContext.ModelState);
        Assert.Equal(expectedKey, modelState.Key);
        var error = Assert.Single(modelState.Value.Errors);
        Assert.Equal(attribute.FormatErrorMessage(expectedFieldName), error.ErrorMessage);
        Assert.Equal(4, sink.Writes.Count());
    }

    [Fact]
    public async Task BindModelAsync_EnforcesTopLevelDataAnnotationsAttribute()
    {
        // Arrange
        var actionContext = GetControllerContext();
        var mockModelMetadata = CreateMockModelMetadata();
        var validationAttribute = new RangeAttribute(1, 100);
        mockModelMetadata.Setup(o => o.DisplayName).Returns("My Display Name");
        mockModelMetadata.Setup(o => o.ValidatorMetadata).Returns(new[] {
                validationAttribute
            });

        var validator = new DataAnnotationsModelValidator(
            new ValidationAttributeAdapterProvider(),
            validationAttribute,
            stringLocalizer: null);

        var parameterBinder = CreateParameterBinder(mockModelMetadata.Object, validator);
        var modelBindingResult = ModelBindingResult.Success(123);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            CreateMockModelBinder(modelBindingResult),
            CreateMockValueProvider(),
            new ParameterDescriptor { Name = "myParam", ParameterType = typeof(Person) },
            mockModelMetadata.Object,
            50); // This value is ignored, because test explicitly set the ModelBindingResult

        // Assert
        Assert.False(actionContext.ModelState.IsValid);
        Assert.Equal("myParam", actionContext.ModelState.Single().Key);
        Assert.Equal(
            validationAttribute.FormatErrorMessage("My Display Name"),
            actionContext.ModelState.Single().Value.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task BindModelAsync_SupportsIObjectModelValidatorForBackCompat()
    {
        // Arrange
        var actionContext = GetControllerContext();

        var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
        mockValidator
            .Setup(o => o.Validate(
                It.IsAny<ActionContext>(),
                It.IsAny<ValidationStateDictionary>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Callback((ActionContext context, ValidationStateDictionary validationState, string prefix, object model) =>
            {
                context.ModelState.AddModelError(prefix, "Test validation message");
            });

        var modelMetadata = CreateMockModelMetadata().Object;
        var parameterBinder = CreateBackCompatParameterBinder(
            modelMetadata,
            mockValidator.Object);
        var modelBindingResult = ModelBindingResult.Success(123);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            CreateMockModelBinder(modelBindingResult),
            CreateMockValueProvider(),
            new ParameterDescriptor { Name = "myParam", ParameterType = typeof(Person) },
            modelMetadata,
            "ignored");

        // Assert
        Assert.False(actionContext.ModelState.IsValid);
        Assert.Equal("myParam", actionContext.ModelState.Single().Key);
        Assert.Equal(
            "Test validation message",
            actionContext.ModelState.Single().Value.Errors.Single().ErrorMessage);
    }

    [Fact]
    [ReplaceCulture]
    public async Task BindModelAsync_ForParameter_UsesValidationFromActualModel_WhenDerivedModelIsSet()
    {
        // Arrange
        var method = GetType().GetMethod(nameof(TestMethodWithoutAttributes), BindingFlags.NonPublic | BindingFlags.Instance);
        var parameter = method.GetParameters()[0];
        var parameterDescriptor = new ControllerParameterDescriptor
        {
            ParameterInfo = parameter,
            Name = parameter.Name,
        };

        var actionContext = GetControllerContext();
        var modelMetadataProvider = new TestModelMetadataProvider();

        var model = new DerivedPerson();
        var modelBindingResult = ModelBindingResult.Success(model);

        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            Mock.Of<IModelBinderFactory>(),
            new DefaultObjectValidator(
                modelMetadataProvider,
                new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                new MvcOptions()),
            _optionsAccessor,
            NullLoggerFactory.Instance);

        var modelMetadata = modelMetadataProvider.GetMetadataForParameter(parameter);
        var modelBinder = CreateMockModelBinder(modelBindingResult);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            modelBinder,
            CreateMockValueProvider(),
            parameterDescriptor,
            modelMetadata,
            value: null);

        // Assert
        Assert.True(result.IsModelSet);
        Assert.Same(model, result.Model);

        Assert.False(actionContext.ModelState.IsValid);
        Assert.Collection(
            actionContext.ModelState,
            kvp =>
            {
                Assert.Equal($"{parameter.Name}.{nameof(DerivedPerson.DerivedProperty)}", kvp.Key);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("The DerivedProperty field is required.", error.ErrorMessage);
            });
    }

    [Fact]
    public async Task BindModelAsync_ForParameter_UsesValidationFromParameter_WhenDerivedModelIsSet()
    {
        // Arrange
        var method = GetType().GetMethod(nameof(TestMethodWithAttributes), BindingFlags.NonPublic | BindingFlags.Instance);
        var parameter = method.GetParameters()[0];
        var parameterDescriptor = new ControllerParameterDescriptor
        {
            ParameterInfo = parameter,
            Name = parameter.Name,
        };

        var actionContext = GetControllerContext();
        var modelMetadataProvider = new TestModelMetadataProvider();

        var model = new DerivedPerson { DerivedProperty = "SomeValue" };
        var modelBindingResult = ModelBindingResult.Success(model);

        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            Mock.Of<IModelBinderFactory>(),
            new DefaultObjectValidator(
                modelMetadataProvider,
                new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                new MvcOptions()),
            _optionsAccessor,
            NullLoggerFactory.Instance);

        var modelMetadata = modelMetadataProvider.GetMetadataForParameter(parameter);
        var modelBinder = CreateMockModelBinder(modelBindingResult);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            modelBinder,
            CreateMockValueProvider(),
            parameterDescriptor,
            modelMetadata,
            value: null);

        // Assert
        Assert.True(result.IsModelSet);
        Assert.Same(model, result.Model);

        Assert.False(actionContext.ModelState.IsValid);
        Assert.Collection(
            actionContext.ModelState,
            kvp =>
            {
                Assert.Equal(parameter.Name, kvp.Key);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("Always Invalid", error.ErrorMessage);
            });
    }

    [Fact]
    [ReplaceCulture]
    public async Task BindModelAsync_ForProperty_UsesValidationFromActualModel_WhenDerivedModelIsSet()
    {
        // Arrange
        var property = typeof(TestController).GetProperty(nameof(TestController.Model));
        var parameterDescriptor = new ControllerBoundPropertyDescriptor
        {
            PropertyInfo = property,
            Name = property.Name,
        };

        var actionContext = GetControllerContext();
        var modelMetadataProvider = new TestModelMetadataProvider();

        var model = new DerivedModel();
        var modelBindingResult = ModelBindingResult.Success(model);

        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            Mock.Of<IModelBinderFactory>(),
            new DefaultObjectValidator(
                modelMetadataProvider,
                new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                new MvcOptions()),
            _optionsAccessor,
            NullLoggerFactory.Instance);

        var modelMetadata = modelMetadataProvider.GetMetadataForProperty(property.DeclaringType, property.Name);
        var modelBinder = CreateMockModelBinder(modelBindingResult);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            modelBinder,
            CreateMockValueProvider(),
            parameterDescriptor,
            modelMetadata,
            value: null);

        // Assert
        Assert.True(result.IsModelSet);
        Assert.Same(model, result.Model);

        Assert.False(actionContext.ModelState.IsValid);
        Assert.Collection(
            actionContext.ModelState,
            kvp =>
            {
                Assert.Equal($"{property.Name}.{nameof(DerivedPerson.DerivedProperty)}", kvp.Key);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("The DerivedProperty field is required.", error.ErrorMessage);
            });
    }

    [Fact]
    public async Task BindModelAsync_ForProperty_UsesValidationOnProperty_WhenDerivedModelIsSet()
    {
        // Arrange
        var property = typeof(TestControllerWithValidatedProperties).GetProperty(nameof(TestControllerWithValidatedProperties.Model));
        var parameterDescriptor = new ControllerBoundPropertyDescriptor
        {
            PropertyInfo = property,
            Name = property.Name,
        };

        var actionContext = GetControllerContext();
        var modelMetadataProvider = new TestModelMetadataProvider();

        var model = new DerivedModel { DerivedProperty = "some value" };
        var modelBindingResult = ModelBindingResult.Success(model);

        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            Mock.Of<IModelBinderFactory>(),
            new DefaultObjectValidator(
                modelMetadataProvider,
                new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                new MvcOptions()),
            _optionsAccessor,
            NullLoggerFactory.Instance);

        var modelMetadata = modelMetadataProvider.GetMetadataForProperty(property.DeclaringType, property.Name);
        var modelBinder = CreateMockModelBinder(modelBindingResult);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            modelBinder,
            CreateMockValueProvider(),
            parameterDescriptor,
            modelMetadata,
            value: null);

        // Assert
        Assert.True(result.IsModelSet);
        Assert.Same(model, result.Model);

        Assert.False(actionContext.ModelState.IsValid);
        Assert.Collection(
            actionContext.ModelState,
            kvp =>
            {
                Assert.Equal($"{property.Name}", kvp.Key);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("Always Invalid", error.ErrorMessage);
            });
    }

    // Regression test 1 for aspnet/Mvc#7963. ModelState should never be valid.
    [Fact]
    public async Task BindModelAsync_ForOverlappingParametersWithSuppressions_InValid_WithValidSecondParameter()
    {
        // Arrange
        var parameterDescriptor = new ParameterDescriptor
        {
            Name = "patchDocument",
            ParameterType = typeof(IJsonPatchDocument),
        };

        var actionContext = GetControllerContext();
        var modelState = actionContext.ModelState;

        // First ModelState key is not empty to match SimpleTypeModelBinder.
        modelState.SetModelValue("id", "notAGuid", "notAGuid");
        modelState.AddModelError("id", "This is not valid.");

        var modelMetadataProvider = new TestModelMetadataProvider();
        modelMetadataProvider.ForType<IJsonPatchDocument>().ValidationDetails(v => v.ValidateChildren = false);
        var modelMetadata = modelMetadataProvider.GetMetadataForType(typeof(IJsonPatchDocument));

        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            Mock.Of<IModelBinderFactory>(),
            new DefaultObjectValidator(
                modelMetadataProvider,
                new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                new MvcOptions()),
            _optionsAccessor,
            NullLoggerFactory.Instance);

        // BodyModelBinder does not update ModelState in success case.
        var modelBindingResult = ModelBindingResult.Success(new JsonPatchDocument());
        var modelBinder = CreateMockModelBinder(modelBindingResult);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            modelBinder,
            new SimpleValueProvider(),
            parameterDescriptor,
            modelMetadata,
            value: null);

        // Assert
        Assert.True(result.IsModelSet);
        Assert.False(modelState.IsValid);
        Assert.Collection(
            modelState,
            kvp =>
            {
                Assert.Equal("id", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("This is not valid.", error.ErrorMessage);
            });
    }

    // Regression test 2 for aspnet/Mvc#7963. ModelState should never be valid.
    [Fact]
    public async Task BindModelAsync_ForOverlappingParametersWithSuppressions_InValid_WithInValidSecondParameter()
    {
        // Arrange
        var parameterDescriptor = new ParameterDescriptor
        {
            Name = "patchDocument",
            ParameterType = typeof(IJsonPatchDocument),
        };

        var actionContext = GetControllerContext();
        var modelState = actionContext.ModelState;

        // First ModelState key is not empty to match SimpleTypeModelBinder.
        modelState.SetModelValue("id", "notAGuid", "notAGuid");
        modelState.AddModelError("id", "This is not valid.");

        // Second ModelState key is empty to match BodyModelBinder.
        modelState.AddModelError(string.Empty, "This is also not valid.");

        var modelMetadataProvider = new TestModelMetadataProvider();
        modelMetadataProvider.ForType<IJsonPatchDocument>().ValidationDetails(v => v.ValidateChildren = false);
        var modelMetadata = modelMetadataProvider.GetMetadataForType(typeof(IJsonPatchDocument));

        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            Mock.Of<IModelBinderFactory>(),
            new DefaultObjectValidator(
                modelMetadataProvider,
                new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                new MvcOptions()),
            _optionsAccessor,
            NullLoggerFactory.Instance);

        var modelBindingResult = ModelBindingResult.Failed();
        var modelBinder = CreateMockModelBinder(modelBindingResult);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            modelBinder,
            new SimpleValueProvider(),
            parameterDescriptor,
            modelMetadata,
            value: null);

        // Assert
        Assert.False(result.IsModelSet);
        Assert.False(modelState.IsValid);
        Assert.Collection(
            modelState,
            kvp =>
            {
                Assert.Empty(kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("This is also not valid.", error.ErrorMessage);
            },
            kvp =>
            {
                Assert.Equal("id", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("This is not valid.", error.ErrorMessage);
            });
    }

    // Regression test for aspnet/Mvc#8078. Later parameter should not mark entry as valid.
    [Fact]
    public async Task BindModelAsync_ForOverlappingParameters_InValid_WithInValidFirstParameterAndSecondNull()
    {
        // Arrange
        var parameterDescriptor = new ParameterDescriptor
        {
            BindingInfo = new BindingInfo
            {
                BinderModelName = "id",
            },
            Name = "identifier",
            ParameterType = typeof(string),
        };

        var actionContext = GetControllerContext();
        var modelState = actionContext.ModelState;

        // Mimic ModelStateEntry when first parameter is [FromRoute] int id and request URI is /api/values/notAnInt
        modelState.SetModelValue("id", "notAnInt", "notAnInt");
        modelState.AddModelError("id", "This is not valid.");

        var modelMetadataProvider = new TestModelMetadataProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForType(typeof(string));
        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            Mock.Of<IModelBinderFactory>(),
            new DefaultObjectValidator(
                modelMetadataProvider,
                new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                new MvcOptions()),
            _optionsAccessor,
            NullLoggerFactory.Instance);

        // Mimic result when second parameter is [FromQuery(Name = "id")] string identifier and query is ?id
        var modelBindingResult = ModelBindingResult.Success(null);
        var modelBinder = CreateMockModelBinder(modelBindingResult);

        // Act
        var result = await parameterBinder.BindModelAsync(
            actionContext,
            modelBinder,
            new SimpleValueProvider(),
            parameterDescriptor,
            modelMetadata,
            value: null);

        // Assert
        Assert.True(result.IsModelSet);
        Assert.False(modelState.IsValid);
        var keyValuePair = Assert.Single(modelState);
        Assert.Equal("id", keyValuePair.Key);
        Assert.Equal(ModelValidationState.Invalid, keyValuePair.Value.ValidationState);
    }

    private static ControllerContext GetControllerContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = services.BuildServiceProvider()
            }
        };
    }

    private static Mock<FakeModelMetadata> CreateMockModelMetadata()
    {
        var mockModelMetadata = new Mock<FakeModelMetadata>();
        mockModelMetadata
            .Setup(o => o.ModelBindingMessageProvider)
            .Returns(new DefaultModelBindingMessageProvider());
        return mockModelMetadata;
    }

    private static IModelBinder CreateMockModelBinder(ModelBindingResult modelBinderResult)
    {
        var mockBinder = new Mock<IModelBinder>(MockBehavior.Strict);
        mockBinder
            .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
            .Returns<ModelBindingContext>(context =>
            {
                context.Result = modelBinderResult;
                return Task.CompletedTask;
            });
        return mockBinder.Object;
    }

    private static ParameterBinder CreateParameterBinder(
        ModelMetadata modelMetadata,
        IModelValidator validator = null,
        IOptions<MvcOptions> optionsAccessor = null,
        ILoggerFactory loggerFactory = null)
    {
        var mockModelMetadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
        mockModelMetadataProvider
            .Setup(o => o.GetMetadataForType(typeof(Person)))
            .Returns(modelMetadata);

        var mockModelBinderFactory = new Mock<IModelBinderFactory>(MockBehavior.Strict);
        optionsAccessor = optionsAccessor ?? _optionsAccessor;
        return new ParameterBinder(
            mockModelMetadataProvider.Object,
            mockModelBinderFactory.Object,
            new DefaultObjectValidator(
                mockModelMetadataProvider.Object,
                new[] { GetModelValidatorProvider(validator) },
                new MvcOptions()),
            optionsAccessor,
            loggerFactory ?? NullLoggerFactory.Instance);
    }

    private static IModelValidatorProvider GetModelValidatorProvider(IModelValidator validator = null)
    {
        if (validator == null)
        {
            validator = Mock.Of<IModelValidator>();
        }

        var validatorProvider = new Mock<IModelValidatorProvider>();
        validatorProvider
            .Setup(p => p.CreateValidators(It.IsAny<ModelValidatorProviderContext>()))
            .Callback<ModelValidatorProviderContext>(context =>
            {
                foreach (var result in context.Results)
                {
                    result.Validator = validator;
                    result.IsReusable = true;
                }
            });
        return validatorProvider.Object;
    }

    private static ParameterBinder CreateBackCompatParameterBinder(
        ModelMetadata modelMetadata,
        IObjectModelValidator validator)
    {
        var mockModelMetadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
        mockModelMetadataProvider
            .Setup(o => o.GetMetadataForType(typeof(Person)))
            .Returns(modelMetadata);

        var mockModelBinderFactory = new Mock<IModelBinderFactory>(MockBehavior.Strict);
        return new ParameterBinder(
            mockModelMetadataProvider.Object,
            mockModelBinderFactory.Object,
            validator,
            _optionsAccessor,
            NullLoggerFactory.Instance);
    }

    private static IValueProvider CreateMockValueProvider()
    {
        var mockValueProvider = new Mock<IValueProvider>(MockBehavior.Strict);
        mockValueProvider
            .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
            .Returns(true);
        return mockValueProvider.Object;
    }

    private class Person : IEquatable<Person>, IEquatable<object>
    {
        public string Name { get; set; }

        public bool Equals(Person other)
        {
            return other != null && string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        bool IEquatable<object>.Equals(object obj)
        {
            return Equals(obj as Person);
        }
    }

    private class Family
    {
        public Person Dad { get; set; }

        public Person Mom { get; set; }

        public IList<Person> Kids { get; } = new List<Person>();
    }

    private class DerivedPerson : Person
    {
        [Required]
        public string DerivedProperty { get; set; }
    }

    public abstract class FakeModelMetadata : ModelMetadata
    {
        public FakeModelMetadata()
            : base(ModelMetadataIdentity.ForType(typeof(string)))
        {
        }
    }

    private void TestMethodWithoutAttributes(Person person) { }

    private void TestMethodWithAttributes([Required][AlwaysInvalid] Person person) { }

    private class TestController
    {
        public BaseModel Model { get; set; }
    }

    private class TestControllerWithValidatedProperties
    {
        [AlwaysInvalid]
        [Required]
        public BaseModel Model { get; set; }
    }

    private class BaseModel
    {
    }

    private class DerivedModel
    {
        [Required]
        public string DerivedProperty { get; set; }
    }

    private class AlwaysInvalidAttribute : ValidationAttribute
    {
        public AlwaysInvalidAttribute()
        {
            ErrorMessage = "Always Invalid";
        }

        public override bool IsValid(object value)
        {
            return false;
        }
    }
}
