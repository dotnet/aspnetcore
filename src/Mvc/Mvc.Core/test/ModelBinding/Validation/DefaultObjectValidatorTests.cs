// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class DefaultObjectValidatorTests
{
    private readonly MvcOptions _options = new MvcOptions();

    private ModelMetadataProvider MetadataProvider { get; } = TestModelMetadataProvider.CreateDefaultProvider();

    [Fact]
    public void Validate_SimpleValueType_Valid_WithPrefix()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)15;

        modelState.SetModelValue("parameter", "15", "15");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        AssertKeysEqual(modelState, "parameter");

        var entry = modelState["parameter"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    public void Validate_SimpleReferenceType_Valid_WithPrefix()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)"test";

        modelState.SetModelValue("parameter", "test", "test");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.True(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter");

        var entry = modelState["parameter"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    public void Validate_SimpleType_MaxErrorsReached()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)"test";

        modelState.MaxAllowedErrors = 1;
        modelState.AddModelError("other.Model", "error");
        modelState.SetModelValue("parameter", "test", "test");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, string.Empty, "parameter");

        var entry = modelState["parameter"];
        Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    public void Validate_SimpleType_SuppressValidation()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)"test";

        modelState.SetModelValue("parameter", "test", "test");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter", SuppressValidation = true });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.True(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter");

        var entry = modelState["parameter"];
        Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    // More like how product code does suppressions than Validate_SimpleType_SuppressValidation()
    [Fact]
    public void Validate_SimpleType_SuppressValidationWithNullKey()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validator = CreateValidator();
        var model = "test";
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry { SuppressValidation = true } }
            };

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);
    }

    [Fact]
    public void Validate_ComplexValueType_Valid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new ValueType() { Reference = "ref", Value = 256 };

        modelState.SetModelValue("parameter.Reference", "ref", "ref");
        modelState.SetModelValue("parameter.Value", "256", "256");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.True(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter.Reference", "parameter.Value");

        var entry = modelState["parameter.Reference"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["parameter.Value"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    public void Validate_ComplexReferenceType_Valid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new ReferenceType() { Reference = "ref", Value = 256 };

        modelState.SetModelValue("parameter.Reference", "ref", "ref");
        modelState.SetModelValue("parameter.Value", "256", "256");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.True(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter.Reference", "parameter.Value");

        var entry = modelState["parameter.Reference"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["parameter.Value"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    public void Validate_ComplexReferenceType_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new Person();

        validationState.Add(model, new ValidationStateEntry() { Key = string.Empty });

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "Name", "Profession");

        var entry = modelState["Name"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Name"), error.ErrorMessage);

        entry = modelState["Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Profession"), error.ErrorMessage);
    }

    [Fact]
    public void Validate_ComplexType_SuppressValidation()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = new Person2()
        {
            Name = "Billy",
            Address = new Address { Street = "GreaterThan5Characters" }
        };

        modelState.SetModelValue("person.Name", "Billy", "Billy");
        modelState.SetModelValue("person.Address.Street", "GreaterThan5Characters", "GreaterThan5Characters");
        validationState.Add(model, new ValidationStateEntry() { Key = "person" });
        validationState.Add(model.Address, new ValidationStateEntry()
        {
            Key = "person.Address",
            SuppressValidation = true
        });

        // Act
        validator.Validate(actionContext, validationState, "person", model);

        // Assert
        Assert.True(modelState.IsValid);
        AssertKeysEqual(modelState, "person.Name", "person.Address.Street");

        var entry = modelState["person.Name"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["person.Address.Street"];
        Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_ComplexReferenceType_Invalid_MultipleErrorsOnProperty()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new Address() { Street = "Microsoft Way" };

        modelState.SetModelValue("parameter.Street", "Microsoft Way", "Microsoft Way");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter.Street");

        var entry = modelState["parameter.Street"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

        Assert.Equal(2, entry.Errors.Count);
        var errorMessages = entry.Errors.Select(e => e.ErrorMessage);
        Assert.Contains(ValidationAttributeUtil.GetStringLengthErrorMessage(null, 5, "Street"), errorMessages);
        Assert.Contains(ValidationAttributeUtil.GetRegExErrorMessage("hehehe", "Street"), errorMessages);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_ComplexReferenceType_Invalid_MultipleErrorsOnProperty_EmptyPrefix()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new Address() { Street = "Microsoft Way" };

        modelState.SetModelValue("Street", "Microsoft Way", "Microsoft Way");
        validationState.Add(model, new ValidationStateEntry() { Key = string.Empty });

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "Street");

        var entry = modelState["Street"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

        Assert.Equal(2, entry.Errors.Count);
        var errorMessages = entry.Errors.Select(e => e.ErrorMessage);
        Assert.Contains(ValidationAttributeUtil.GetStringLengthErrorMessage(null, 5, "Street"), errorMessages);
        Assert.Contains(ValidationAttributeUtil.GetRegExErrorMessage("hehehe", "Street"), errorMessages);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_NestedComplexReferenceType_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new Person() { Name = "Rick", Friend = new Person() };

        modelState.SetModelValue("Name", "Rick", "Rick");
        validationState.Add(model, new ValidationStateEntry() { Key = string.Empty });

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "Name", "Profession", "Friend.Name", "Friend.Profession");

        var entry = modelState["Name"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = modelState["Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Profession"), error.ErrorMessage);

        entry = modelState["Friend.Name"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Name"), error.ErrorMessage);

        entry = modelState["Friend.Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Profession"), error.ErrorMessage);
    }

    [Fact]
    public void Validate_ComplexType_MultipleInvalidProperties()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var model = new InvalidProperties();
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() },
            };

        var validator = CreateValidator();

        // Act
        validator.Validate(actionContext, validationState, prefix: null, model: model);

        // Assert
        Assert.Collection(
            modelState,
            state =>
            {
                Assert.Equal("FirstName", state.Key);
                var error = Assert.Single(state.Value.Errors);
                Assert.Equal("User object lacks some data.", error.ErrorMessage);
            },
            state =>
            {
                Assert.Equal("Address.City", state.Key);
                var error = Assert.Single(state.Value.Errors);
                Assert.Equal("User object lacks some data.", error.ErrorMessage);
            });
    }

    [Fact]
    public void Validate_ComplexType_MultipleInvalidProperties_WithPrefix()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var model = new InvalidProperties();
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry { Key = "invalidProperties" } },
            };

        var validator = CreateValidator();

        // Act
        validator.Validate(actionContext, validationState, prefix: "invalidProperties", model: model);

        // Assert
        Assert.Collection(
            modelState,
            state =>
            {
                Assert.Equal("invalidProperties.FirstName", state.Key);
                var error = Assert.Single(state.Value.Errors);
                Assert.Equal("User object lacks some data.", error.ErrorMessage);
            },
            state =>
            {
                Assert.Equal("invalidProperties.Address.City", state.Key);
                var error = Assert.Single(state.Value.Errors);
                Assert.Equal("User object lacks some data.", error.ErrorMessage);
            });
    }

    // IValidatableObject is significant because the validators are on the object
    // itself, not just the properties.
    [Fact]
    [ReplaceCulture]
    public void Validate_ComplexType_IValidatableObject_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new ValidatableModel();

        modelState.SetModelValue("parameter", "model", "model");

        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter", "parameter.Property1", "parameter.Property2", "parameter.Property3");

        var entry = modelState["parameter"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.Equal("Error1 about '' (display: 'ValidatableModel').", error.ErrorMessage);

        entry = modelState["parameter.Property1"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal("Error2", error.ErrorMessage);

        entry = modelState["parameter.Property2"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal("Error3", error.ErrorMessage);

        entry = modelState["parameter.Property3"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal("Error3", error.ErrorMessage);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_NestedComplexType_IValidatableObject_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new ValidatableModelContainer
        {
            ValidatableModelProperty = new ValidatableModel(),
        };

        modelState.SetModelValue("parameter", "model", "model");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.False(modelState.IsValid);
        Assert.Collection(
            modelState,
            entry =>
            {
                Assert.Equal("parameter", entry.Key);
                Assert.Equal(ModelValidationState.Unvalidated, entry.Value.ValidationState);
                Assert.Empty(entry.Value.Errors);
            },
            entry =>
            {
                Assert.Equal("parameter.ValidatableModelProperty", entry.Key);
                Assert.Equal(ModelValidationState.Invalid, entry.Value.ValidationState);
                var error = Assert.Single(entry.Value.Errors);
                Assert.Equal(
                    "Error1 about 'ValidatableModelProperty' (display: 'Never valid').",
                    error.ErrorMessage);
            },
            entry =>
            {
                Assert.Equal("parameter.ValidatableModelProperty.Property1", entry.Key);
                Assert.Equal(ModelValidationState.Invalid, entry.Value.ValidationState);
                var error = Assert.Single(entry.Value.Errors);
                Assert.Equal("Error2", error.ErrorMessage);
            },
            entry =>
            {
                Assert.Equal("parameter.ValidatableModelProperty.Property2", entry.Key);
                Assert.Equal(ModelValidationState.Invalid, entry.Value.ValidationState);
                var error = Assert.Single(entry.Value.Errors);
                Assert.Equal("Error3", error.ErrorMessage);
            },
            entry =>
            {
                Assert.Equal("parameter.ValidatableModelProperty.Property3", entry.Key);
                Assert.Equal(ModelValidationState.Invalid, entry.Value.ValidationState);
                var error = Assert.Single(entry.Value.Errors);
                Assert.Equal("Error3", error.ErrorMessage);
            });
    }

    [ConditionalFact]
    [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
    public void Validate_ComplexType_IValidatableObject_CanUseRequestServices()
    {
        // Arrange
        var service = new Mock<IExampleService>();
        service.Setup(x => x.DoSomething()).Verifiable();

        var provider = new ServiceCollection().AddSingleton(service.Object).BuildServiceProvider();

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(x => x.RequestServices).Returns(provider);

        var actionContext = new ActionContext { HttpContext = httpContext.Object };

        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = new Mock<IValidatableObject>();
        model
            .Setup(x => x.Validate(It.IsAny<ValidationContext>()))
            .Callback((ValidationContext context) =>
            {
                var receivedService = context.GetService<IExampleService>();
                Assert.Equal(service.Object, receivedService);
                receivedService.DoSomething();
            })
            .Returns(new List<ValidationResult>());

        // Act
        validator.Validate(actionContext, validationState, prefix: null, model: model.Object);

        // Assert
        service.Verify();
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_ComplexType_FieldsAreIgnored_Valid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new VariableTest() { test = 5 };

        modelState.SetModelValue("parameter", "5", "5");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.True(modelState.IsValid);
        Assert.Single(modelState);

        var entry = modelState["parameter"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_ComplexType_SecondLevelCyclesNotFollowed_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var person = new Person() { Name = "Billy" };
        person.Family = new Family { Members = new List<Person> { person } };

        var model = (object)person;

        modelState.SetModelValue("parameter.Name", "Billy", "Billy");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter.Name", "parameter.Profession");

        var entry = modelState["parameter.Name"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["parameter.Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.Equal(error.ErrorMessage, ValidationAttributeUtil.GetRequiredErrorMessage("Profession"));
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_ComplexType_CyclesNotFollowed_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var person = new Person() { Name = "Billy" };
        person.Friend = person;

        var model = (object)person;

        modelState.SetModelValue("parameter.Name", "Billy", "Billy");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter.Name", "parameter.Profession");

        var entry = modelState["parameter.Name"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["parameter.Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.Equal(error.ErrorMessage, ValidationAttributeUtil.GetRequiredErrorMessage("Profession"));
    }

    [Fact]
    public void Validate_ComplexType_ShortCircuit_WhenMaxErrorCountIsSet()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator(typeof(string));

        var model = new User()
        {
            Password = "password-val",
            ConfirmPassword = "not-password-val"
        };

        modelState.MaxAllowedErrors = 2;
        modelState.AddModelError("key1", "error1");
        modelState.SetModelValue("user.Password", "password-val", "password-val");
        modelState.SetModelValue("user.ConfirmPassword", "not-password-val", "not-password-val");

        validationState.Add(model, new ValidationStateEntry() { Key = "user", });

        // Act
        validator.Validate(actionContext, validationState, "user", model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, string.Empty, "key1", "user.ConfirmPassword", "user.Password");

        var entry = modelState[string.Empty];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.IsType<TooManyModelErrorsException>(error.Exception);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_CollectionType_ArrayOfSimpleType_Valid_DefaultKeyPattern()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new int[] { 5, 17 };

        modelState.SetModelValue("parameter[0]", "5", "17");
        modelState.SetModelValue("parameter[1]", "17", "5");
        validationState.Add(model, new ValidationStateEntry() { Key = "parameter" });

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.True(modelState.IsValid);
        AssertKeysEqual(modelState, "parameter[0]", "parameter[1]");

        var entry = modelState["parameter[0]"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["parameter[0]"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_CollectionType_ArrayOfComplexType_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new Person[] { new Person(), new Person() };

        validationState.Add(model, new ValidationStateEntry() { Key = string.Empty });

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "[0].Name", "[0].Profession", "[1].Name", "[1].Profession");

        var entry = modelState["[0].Name"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Name"), error.ErrorMessage);

        entry = modelState["[0].Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Profession"), error.ErrorMessage);

        entry = modelState["[1].Name"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Name"), error.ErrorMessage);

        entry = modelState["[1].Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Profession"), error.ErrorMessage);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_CollectionType_ListOfComplexType_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new List<Person> { new Person(), new Person() };

        validationState.Add(model, new ValidationStateEntry() { Key = string.Empty });

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(modelState, "[0].Name", "[0].Profession", "[1].Name", "[1].Profession");

        var entry = modelState["[0].Name"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Name"), error.ErrorMessage);

        entry = modelState["[0].Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Profession"), error.ErrorMessage);

        entry = modelState["[1].Name"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Name"), error.ErrorMessage);

        entry = modelState["[1].Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Profession"), error.ErrorMessage);
    }

    public static TheoryData<object, Type> ValidCollectionData
    {
        get
        {
            return new TheoryData<object, Type>()
                {
                    { new int[] { 1, 2, 3 }, typeof(int[]) },
                    { new string[] { "Foo", "Bar", "Baz" }, typeof(string[]) },
                    { new List<string> { "Foo", "Bar", "Baz" }, typeof(IList<string>)},
                    { new HashSet<string> { "Foo", "Bar", "Baz" }, typeof(string[]) },
                    {
                        new List<DateTime>
                        {
                            new DateTime(2014, 1, 1),
                            new DateTime(2014, 2, 1),
                            new DateTime(2014, 3, 1),
                        },
                        typeof(ICollection<DateTime>)
                    },
                    {
                        new HashSet<Uri>
                        {
                            new Uri("http://example.com/1"),
                            new Uri("http://example.com/2"),
                            new Uri("http://example.com/3"),
                        },
                        typeof(HashSet<Uri>)
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionData))]
    public void Validate_IndexedCollectionTypes_Valid(object model, Type type)
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        modelState.SetModelValue("items[0]", "value1", "value1");
        modelState.SetModelValue("items[1]", "value2", "value2");
        modelState.SetModelValue("items[2]", "value3", "value3");
        validationState.Add(model, new ValidationStateEntry()
        {
            Key = "items",

            // Force the validator to treat it as the specified type.
            Metadata = MetadataProvider.GetMetadataForType(type),
        });

        // Act
        validator.Validate(actionContext, validationState, "items", model);

        // Assert
        Assert.True(modelState.IsValid);
        AssertKeysEqual(modelState, "items[0]", "items[1]", "items[2]");

        var entry = modelState["items[0]"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["items[1]"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["items[2]"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    public void Validate_CollectionType_MultipleInvalidItems()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var model = new InvalidItemsContainer();
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() },
            };

        var validator = CreateValidator();

        // Act
        validator.Validate(actionContext, validationState, prefix: null, model: model);

        // Assert
        Assert.Collection(
            modelState,
            state =>
            {
                Assert.Equal("Items[0]", state.Key);
                var error = Assert.Single(state.Value.Errors);
                Assert.Equal("Collection contains duplicate value 'Joe'.", error.ErrorMessage);
            },
            state =>
            {
                Assert.Equal("Items[2]", state.Key);
                var error = Assert.Single(state.Value.Errors);
                Assert.Equal("Collection contains duplicate value 'Joe'.", error.ErrorMessage);
            });
    }

    [Fact]
    public void Validate_CollectionType_DictionaryOfSimpleType_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = new Dictionary<string, string>()
            {
                { "FooKey", "FooValue" },
                { "BarKey", "BarValue" }
            };

        modelState.SetModelValue("items[0].Key", "key0", "key0");
        modelState.SetModelValue("items[0].Value", "value0", "value0");
        modelState.SetModelValue("items[1].Key", "key1", "key1");
        modelState.SetModelValue("items[1].Value", "value1", "value1");
        validationState.Add(model, new ValidationStateEntry() { Key = "items" });

        // Act
        validator.Validate(actionContext, validationState, "items", model);

        // Assert
        Assert.True(modelState.IsValid);
        AssertKeysEqual(modelState, "items[0].Key", "items[0].Value", "items[1].Key", "items[1].Value");

        var entry = modelState["items[0].Key"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["items[0].Value"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["items[1].Key"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["items[1].Value"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_CollectionType_DictionaryOfComplexType_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = (object)new Dictionary<string, Person> { { "Joe", new Person() }, { "Mark", new Person() } };

        modelState.SetModelValue("[0].Key", "Joe", "Joe");
        modelState.SetModelValue("[1].Key", "Mark", "Mark");
        validationState.Add(model, new ValidationStateEntry() { Key = string.Empty });

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        Assert.False(modelState.IsValid);
        AssertKeysEqual(
            modelState,
            "[0].Key",
            "[0].Value.Name",
            "[0].Value.Profession",
            "[1].Key",
            "[1].Value.Name",
            "[1].Value.Profession");

        var entry = modelState["[0].Key"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["[1].Key"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.Empty(entry.Errors);

        entry = modelState["[0].Value.Name"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        var error = Assert.Single(entry.Errors);
        Assert.Equal(error.ErrorMessage, ValidationAttributeUtil.GetRequiredErrorMessage("Name"));

        entry = modelState["[0].Value.Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(error.ErrorMessage, ValidationAttributeUtil.GetRequiredErrorMessage("Profession"));

        entry = modelState["[1].Value.Name"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(error.ErrorMessage, ValidationAttributeUtil.GetRequiredErrorMessage("Name"));

        entry = modelState["[1].Value.Profession"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        error = Assert.Single(entry.Errors);
        Assert.Equal(error.ErrorMessage, ValidationAttributeUtil.GetRequiredErrorMessage("Profession"));
    }

    [Fact]
    [ReplaceCulture]
    public void Validate_DoesntCatchExceptions_FromPropertyAccessors()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = new ThrowingProperty();

        // Act & Assert
        Assert.Throws<InvalidTimeZoneException>(
            () =>
            {
                validator.Validate(actionContext, validationState, string.Empty, model);
            });
    }

    // We use the reference equality comparer for breaking cycles
    [Fact]
    public void Validate_DoesNotUseOverridden_GetHashCodeOrEquals()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator();

        var model = new TypeThatOverridesEquals[]
        {
                new TypeThatOverridesEquals { Funny = "hehe" },
                new TypeThatOverridesEquals { Funny = "hehe" }
        };

        // Act & Assert (does not throw)
        validator.Validate(actionContext, validationState, string.Empty, model);
    }

    [Fact]
    public void Validate_ForExcludedComplexType_PropertiesMarkedAsSkipped()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator(typeof(User));

        var model = new User()
        {
            Password = "password-val",
            ConfirmPassword = "not-password-val"
        };

        // Note that user.ConfirmPassword has no entry in modelstate - we should not
        // create one just to mark it as skipped.
        modelState.SetModelValue("user.Password", "password-val", "password-val");
        validationState.Add(model, new ValidationStateEntry() { Key = "user", });

        // Act
        validator.Validate(actionContext, validationState, "user", model);

        // Assert
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);
        AssertKeysEqual(modelState, "user.Password");

        var entry = modelState["user.Password"];
        Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    [Fact]
    public void Validate_ForExcludedCollectionType_PropertiesMarkedAsSkipped()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        var validationState = new ValidationStateDictionary();

        var validator = CreateValidator(typeof(List<ValidatedModel>));

        var model = new List<ValidatedModel>()
            {
                new ValidatedModel { Value = "15" },
            };

        modelState.SetModelValue("userIds[0]", "15", "15");
        validationState.Add(model, new ValidationStateEntry() { Key = "userIds", });

        // Act
        validator.Validate(actionContext, validationState, "userIds", model);

        // Assert
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);
        AssertKeysEqual(modelState, "userIds[0]");

        var entry = modelState["userIds[0]"];
        Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);
        Assert.Empty(entry.Errors);
    }

    private class ValidatedModel
    {
        [Required]
        public string Value { get; set; }
    }

    [Fact]
    public void Validate_SuppressesValidation_ForExcludedType_Stream()
    {
        // Arrange
        var options = new MvcOptions();
        var optionsSetup = new MvcCoreMvcOptionsSetup(Mock.Of<IHttpRequestStreamReaderFactory>());
        optionsSetup.Configure(options);
        var validator = CreateValidator(providers: options.ModelMetadataDetailsProviders.ToArray());
        var model = new MemoryStream(Encoding.UTF8.GetBytes("Hello!"));
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        modelState.SetModelValue("parameter", rawValue: null, attemptedValue: null);
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() { Key = "parameter" } }
            };

        // Act
        validator.Validate(actionContext, validationState, "parameter", model);

        // Assert
        Assert.True(modelState.IsValid);
        var entry = Assert.Single(modelState);
        Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
        Assert.Empty(entry.Value.Errors);
    }

    // Regression test for aspnet/Mvc#7992
    [Fact]
    public void Validate_SuppressValidation_AfterHasReachedMaxErrors_Invalid()
    {
        // Arrange
        var actionContext = new ActionContext();
        var modelState = actionContext.ModelState;
        modelState.MaxAllowedErrors = 2;
        modelState.AddModelError(key: "one", errorMessage: "1");
        modelState.AddModelError(key: "two", errorMessage: "2");

        var validator = CreateValidator();
        var model = (object)23; // Box ASAP
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry { SuppressValidation = true } }
            };

        // Act
        validator.Validate(actionContext, validationState, prefix: string.Empty, model);

        // Assert
        Assert.False(modelState.IsValid);
        Assert.True(modelState.HasReachedMaxErrors);
        Assert.Collection(
            modelState,
            kvp =>
            {
                Assert.Empty(kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.IsType<TooManyModelErrorsException>(error.Exception);
            },
            kvp =>
            {
                Assert.Equal("one", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("1", error.ErrorMessage);
            });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Validate_Throws_IfValidationDepthExceedsMaxDepth(int maxDepth)
    {
        // Arrange
        var expected = $"ValidationVisitor exceeded the maximum configured validation depth '{maxDepth}' when validating property '{nameof(DepthObject.Depth)}' on type '{typeof(DepthObject)}'. " +
            "This may indicate a very deep or infinitely recursive object graph. Consider modifying 'MvcOptions.MaxValidationDepth' or suppressing validation on the model type.";
        _options.MaxValidationDepth = maxDepth;
        var actionContext = new ActionContext();
        var validator = CreateValidator();
        var model = new DepthObject(maxDepth);
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() }
            };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(actionContext, validationState, prefix: string.Empty, model));
        Assert.Equal(expected, ex.Message);
        Assert.Equal("https://aka.ms/AA21ue1", ex.HelpLink);
    }

    [Fact]
    public void Validate_WorksIfObjectGraphIsSmallerThanMaxDepth()
    {
        // Arrange
        var maxDepth = 5;
        _options.MaxValidationDepth = maxDepth;
        var actionContext = new ActionContext();
        var validator = CreateValidator();
        var model = new DepthObject(maxDepth - 1);
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() }
            };

        // Act & Assert
        validator.Validate(actionContext, validationState, prefix: string.Empty, model);
        Assert.True(actionContext.ModelState.IsValid);
    }

    [Fact]
    public void Validate_DoesNotThrow_IfNumberOfErrorsAfterReachingMaxAllowedErrors_GoesOverMaxDepth()
    {
        // Arrange
        var maxDepth = 4;
        _options.MaxValidationDepth = maxDepth;
        var actionContext = new ActionContext();
        actionContext.ModelState.MaxAllowedErrors = 2;
        var validator = CreateValidator();
        var model = new List<ModelWithRequiredProperty>
            {
                new ModelWithRequiredProperty(), new ModelWithRequiredProperty(),
                // After the first 2 items we will reach MaxAllowedErrors
                // If we add items without popping after having reached max validation,
                // with 4 more items (on top of the list) we would go over max depth of 4
                new ModelWithRequiredProperty(), new ModelWithRequiredProperty(),
                new ModelWithRequiredProperty(), new ModelWithRequiredProperty(),
            };

        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() }
            };

        // Act & Assert
        validator.Validate(actionContext, validationState, prefix: string.Empty, model);
        Assert.False(actionContext.ModelState.IsValid);
    }

    [Theory]
    [InlineData(false, ModelValidationState.Unvalidated)]
    [InlineData(true, ModelValidationState.Invalid)]
    public void Validate_RespectsMvcOptionsConfiguration_WhenChildValidationFails(bool optionValue, ModelValidationState expectedParentValidationState)
    {
        // Arrange
        _options.ValidateComplexTypesIfChildValidationFails = optionValue;

        var actionContext = new ActionContext();
        var validationState = new ValidationStateDictionary();
        var validator = CreateValidator();

        var model = (object)new SelfValidatableModelContainer
        {
            IsParentValid = false,
            ValidatableModelProperty = new ValidatableModel()
        };

        // Act
        validator.Validate(actionContext, validationState, prefix: string.Empty, model);

        // Assert
        var modelState = actionContext.ModelState;
        Assert.False(modelState.IsValid);
        Assert.Equal(expectedParentValidationState, modelState.Root.ValidationState);
    }

    [Fact]
    public void Validate_TypeWithoutValidators()
    {
        var actionContext = new ActionContext();
        var validator = CreateValidator();
        var model = new ModelWithoutValidation();
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() }
            };

        actionContext.ModelState.SetModelValue("Property1", new ValueProviderResult("value1"));
        actionContext.ModelState.SetModelValue("Property2", new ValueProviderResult("value2"));

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        var modelState = actionContext.ModelState;
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);
        Assert.True(modelState.IsValid);

        var entry = modelState["Property1"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = modelState["Property2"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    [Fact]
    public void Validate_TypeWithoutValidators_DoesNotUpdateValidationState()
    {
        var actionContext = new ActionContext();
        var validator = CreateValidator();
        var model = new ModelWithoutValidation();
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() }
            };

        var modelState = actionContext.ModelState;
        modelState.SetModelValue("Property1", new ValueProviderResult("value1"));
        modelState.SetModelValue("Property2", new ValueProviderResult("value2"));
        modelState["Property2"].ValidationState = ModelValidationState.Skipped;

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);
        Assert.True(modelState.IsValid);

        var entry = modelState["Property1"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = modelState["Property2"];
        Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);
    }

    [Fact]
    public void Validate_TypeWithoutValidators_DoesNotResetInvalidState()
    {
        var actionContext = new ActionContext();
        var validator = CreateValidator();
        var model = new ModelWithoutValidation();
        var validationState = new ValidationStateDictionary
            {
                { model, new ValidationStateEntry() }
            };

        var modelState = actionContext.ModelState;
        modelState.SetModelValue("Property1", new ValueProviderResult("value1"));
        modelState.SetModelValue("Property2", new ValueProviderResult("value2"));
        modelState["Property2"].ValidationState = ModelValidationState.Invalid;

        // Act
        validator.Validate(actionContext, validationState, string.Empty, model);

        // Assert
        Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);
        Assert.False(modelState.IsValid);

        var entry = modelState["Property1"];
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = modelState["Property2"];
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
    }

    public class ModelWithRequiredProperty
    {
        [Required]
        public string MyProperty { get; set; }
    }

    private class ModelWithoutValidation
    {
        public string Property1 { get; set; }

        public string Property2 { get; set; }
    }

    private static DefaultObjectValidator CreateValidator(Type excludedType)
    {
        var excludeFilters = new List<SuppressChildValidationMetadataProvider>();
        if (excludedType != null)
        {
            excludeFilters.Add(new SuppressChildValidationMetadataProvider(excludedType));
        }

        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider(excludeFilters.ToArray());
        var validatorProviders = TestModelValidatorProvider.CreateDefaultProvider().ValidatorProviders;
        return new DefaultObjectValidator(metadataProvider, validatorProviders, new MvcOptions());
    }

    private DefaultObjectValidator CreateValidator(params IMetadataDetailsProvider[] providers)
    {
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider(providers);
        var validatorProviders = TestModelValidatorProvider.CreateDefaultProvider().ValidatorProviders;
        return new DefaultObjectValidator(metadataProvider, validatorProviders, _options);
    }

    private static void AssertKeysEqual(ModelStateDictionary modelState, params string[] keys)
    {
        Assert.Equal<string>(keys.OrderBy(k => k).ToList(), modelState.Keys.OrderBy(k => k).ToList());
    }

    private class ThrowingProperty
    {
        [Required]
        public string WatchOut
        {
            get
            {
                throw new InvalidTimeZoneException();
            }
        }
    }

    private class Person
    {
        [Required, StringLength(10)]
        public string Name { get; set; }

        [Required]
        public string Profession { get; set; }

        public Person Friend { get; set; }

        public Family Family { get; set; }
    }

    private class Family
    {
        public List<Person> Members { get; set; }
    }

    private class Person2
    {
        public string Name { get; set; }
        public Address Address { get; set; }
    }

    private class Address
    {
        [StringLength(5)]
        [RegularExpression("hehehe")]
        public string Street { get; set; }
    }

    private struct ValueType
    {
        public int Value { get; set; }
        public string Reference { get; set; }
    }

    private class ReferenceType
    {
        public int Value { get; set; }
        public string Reference { get; set; }
    }

    private class ValidatableModel : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield return new ValidationResult(
                $"Error1 about '{validationContext.MemberName}' (display: '{validationContext.DisplayName}').",
                new string[] { });
            yield return new ValidationResult("Error2", new[] { "Property1" });
            yield return new ValidationResult("Error3", new[] { "Property2", "Property3" });
        }
    }

    private class ValidatableModelContainer
    {
        [Display(Name = "Never valid")]
        public ValidatableModel ValidatableModelProperty { get; set; }
    }

    private class SelfValidatableModelContainer : IValidatableObject
    {
        public bool IsParentValid { get; set; } = true;

        [Display(Name = "Never valid")]
        public ValidatableModel ValidatableModelProperty { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsParentValid)
            {
                yield return new ValidationResult("Parent not valid");
            }
        }
    }

    private class TypeThatOverridesEquals
    {
        [StringLength(2)]
        public string Funny { get; set; }

        public override bool Equals(object obj)
        {
            throw new InvalidOperationException();
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException();
        }
    }

    private class VariableTest
    {
        [Range(15, 25)]
        public int test;
    }

    private class User : IValidatableObject
    {
        public string Password { get; set; }

        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Password == "password")
            {
                yield return new ValidationResult("Password does not meet complexity requirements.");
            }
        }
    }

    public interface IExampleService
    {
        void DoSomething();
    }

    private void Validate_Throws_ForTopLevelMetadataData(DepthObject depthObject) { }

    // Custom validation attribute that returns multiple entries in ValidationResult.MemberNames and those member
    // names are indexers. An example scenario is an attribute that confirms all entries in a list are unique.
    private class InvalidItemsAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return new ValidationResult(
                "Collection contains duplicate value 'Joe'.",
                new[] { "[0]", "[2]" });
        }
    }

    private class InvalidItemsContainer
    {
        [InvalidItems]
        public List<string> Items { get; set; } = new List<string> { "Joe", "Fred", "Joe", "Herman" };
    }

    // Custom validation attribute that returns multiple entries in ValidationResult.MemberNames. An example
    // scenario is an attribute that confirms all properties in a complex type are non-empty.
    private class InvalidPropertiesAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return new ValidationResult(
                "User object lacks some data.",
                new[] { "FirstName", "Address.City" });
        }
    }

    [InvalidProperties]
    private class InvalidProperties
    {
        public string FirstName { get; set; }

        public string LastName { get; set; } = "IsSet";

        public InvalidAddress Address { get; set; }
    }

    private class InvalidAddress
    {
        public string City { get; set; }
    }

    private class DepthObject
    {
        public DepthObject(int maxAllowedDepth, int depth = 0)
        {
            MaxAllowedDepth = maxAllowedDepth;
            Depth = depth;
        }

        [Range(-10, 400)]
        public int Depth { get; }
        public int MaxAllowedDepth { get; }

        public DepthObject Instance
        {
            get
            {
                if (Depth == MaxAllowedDepth - 1)
                {
                    return this;
                }

                return new DepthObject(MaxAllowedDepth, Depth + 1);
            }
        }
    }
}
