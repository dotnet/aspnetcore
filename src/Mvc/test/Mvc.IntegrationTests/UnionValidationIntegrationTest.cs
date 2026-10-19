// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class UnionValidationIntegrationTest
{
    [Fact]
    public void Validate_ActiveCase_UsesTypeLevelValidation()
    {
        using var services = CreateServices();
        var context = CreateContext();
        var model = new ValidatableUnion(new ValidatableCase("invalid"));

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.False(context.ModelState.IsValid);
        var entry = Assert.Single(context.ModelState);
        Assert.Equal("model.Value", entry.Key);
        Assert.Equal("Invalid case.", Assert.Single(entry.Value.Errors).ErrorMessage);
    }

    [Fact]
    public void Validate_ExplicitUnionValue_UsesTypeLevelValidation()
    {
        using var services = CreateServices();
        var context = CreateContext();
        var model = new ExplicitUnion(new ValidatableCase("invalid"));

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.False(context.ModelState.IsValid);
        var entry = Assert.Single(context.ModelState);
        Assert.Equal("model.Value", entry.Key);
        Assert.Equal("Invalid case.", Assert.Single(entry.Value.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_ValidationStateSuppressesUnionOrCase(bool suppressCase)
    {
        using var services = CreateServices();
        var context = CreateContext();
        var value = new ValidatableCase("invalid");
        object model = new ValidatableUnion(value);
        context.ModelState.SetModelValue("model.Value.Name", "invalid", "invalid");
        var validationState = new ValidationStateDictionary
        {
            [suppressCase ? value : model] = new ValidationStateEntry
            {
                Key = suppressCase ? "model.Value" : "model",
                SuppressValidation = true,
            },
        };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, validationState, "model", model);

        Assert.True(context.ModelState.IsValid);
        Assert.Equal(ModelValidationState.Skipped, context.ModelState["model.Value.Name"].ValidationState);
    }

    [Fact]
    public void Validate_ValidationStateStrategy_OverridesUnionTraversal()
    {
        using var services = CreateServices();
        var context = CreateContext();
        object model = new ValidatableUnion(new ValidatableCase("invalid"));
        var strategy = new Mock<IValidationStrategy>();
        strategy.Setup(s => s.GetChildren(It.IsAny<ModelMetadata>(), "model", model))
            .Returns(() => Enumerable.Empty<ValidationEntry>().GetEnumerator());
        var validationState = new ValidationStateDictionary
        {
            [model] = new ValidationStateEntry { Strategy = strategy.Object },
        };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, validationState, "model", model);

        Assert.True(context.ModelState.IsValid);
        strategy.Verify(s => s.GetChildren(It.IsAny<ModelMetadata>(), "model", model), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_ValidationStateMetadata_OverridesActiveCaseMetadata(bool validateCase)
    {
        using var services = CreateServices();
        var context = CreateContext();
        var value = new ValidatableCase("invalid");
        var model = new ValidatableUnion(value);
        var metadataProvider = services.GetRequiredService<IModelMetadataProvider>();
        var validationState = new ValidationStateDictionary
        {
            [value] = new ValidationStateEntry
            {
                Key = "overridden",
                Metadata = metadataProvider.GetMetadataForType(validateCase ? typeof(ValidatableCase) : typeof(object)),
            },
        };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, validationState, "model", model);

        Assert.Equal(!validateCase, context.ModelState.IsValid);
        if (validateCase)
        {
            var entry = Assert.Single(context.ModelState);
            Assert.Equal("overridden", entry.Key);
            Assert.Equal("Invalid case.", Assert.Single(entry.Value.Errors).ErrorMessage);
        }
        else
        {
            Assert.Empty(context.ModelState);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Validate_ValidationStateMetadata_OverridesUnionMetadata(bool nested, bool existingError)
    {
        using var services = CreateServices();
        var context = CreateContext();
        var value = new ExplicitUnion(new ValidatableCase("invalid"));
        object model = nested ? new ExplicitUnion(value) : value;
        var key = nested ? "model.Value" : "model";
        if (existingError)
        {
            context.ModelState.AddModelError(key, "Existing error.");
        }

        var validationState = new ValidationStateDictionary
        {
            [value] = new ValidationStateEntry
            {
                Metadata = services.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(typeof(object)),
            },
        };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, validationState, "model", model);

        Assert.Equal(!existingError, context.ModelState.IsValid);
        Assert.Equal(existingError ? 1 : 0, context.ModelState.ErrorCount);
        if (existingError)
        {
            var entry = Assert.Single(context.ModelState);
            Assert.Equal(key, entry.Key);
            Assert.Equal("Existing error.", Assert.Single(entry.Value.Errors).ErrorMessage);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_ValidationStateMetadata_PreservesUnionTraversal(bool nullable)
    {
        using var services = CreateServices();
        var context = CreateContext();
        object model = new ValidatableUnion(new ValidatableCase("invalid"));
        var validationState = new ValidationStateDictionary
        {
            [model] = new ValidationStateEntry
            {
                Metadata = services.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(nullable ? typeof(ValidatableUnion?) : typeof(ValidatableUnion)),
            },
        };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, validationState, "model", model);

        Assert.False(context.ModelState.IsValid);
        var entry = Assert.Single(context.ModelState);
        Assert.Equal("model.Value", entry.Key);
        Assert.Equal("Invalid case.", Assert.Single(entry.Value.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Validate_DecoratedMetadataProvider_PreservesValuePropertyAndCaseValidators(bool decorate, bool invalidCase)
    {
        using var services = CreateServices(decorateMetadataProvider: decorate);
        var context = CreateContext();
        object model = invalidCase
            ? new ValidatableUnion(new ValidatableCase("invalid"))
            : new LengthUnion("long");

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.False(context.ModelState.IsValid);
        var entry = Assert.Single(context.ModelState);
        Assert.Equal("model.Value", entry.Key);
        Assert.Equal(invalidCase ? "Invalid case." : "The field Value must be a string or array type with a maximum length of '3'.", Assert.Single(entry.Value.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData("long", false)]
    [InlineData("ok", true)]
    public void Validate_TopLevelUnionValue_PreservesPropertyValidators(string value, bool isValid)
    {
        using var services = CreateServices();
        var context = CreateContext();
        var model = new LengthUnion(value);
        var metadata = services.GetRequiredService<IModelMetadataProvider>().GetMetadataForProperties(typeof(LengthUnion))
            .Single(property => property.PropertyName == nameof(LengthUnion.Value));

        ((ObjectModelValidator)services.GetRequiredService<IObjectModelValidator>()).Validate(
            context, null, "model.Value", model.Value, metadata, container: model);

        Assert.Equal(isValid, context.ModelState.IsValid);
        if (isValid)
        {
            Assert.Empty(context.ModelState);
        }
        else
        {
            var entry = Assert.Single(context.ModelState);
            Assert.Equal("model.Value", entry.Key);
            Assert.Equal("The field Value must be a string or array type with a maximum length of '3'.", Assert.Single(entry.Value.Errors).ErrorMessage);
        }
    }

    [Fact]
    public void Validate_TopLevelUnionValue_UsesTypeLevelValidation()
    {
        using var services = CreateServices();
        var context = CreateContext();
        var value = new ValidatableCase("invalid");
        var model = new ValidatableUnion(value);
        var metadata = services.GetRequiredService<IModelMetadataProvider>().GetMetadataForProperties(typeof(ValidatableUnion))
            .Single(property => property.PropertyName == nameof(IUnion.Value));

        ((ObjectModelValidator)services.GetRequiredService<IObjectModelValidator>()).Validate(
            context, null, "model.Value", value, metadata, container: model);

        Assert.False(context.ModelState.IsValid);
        var entry = Assert.Single(context.ModelState);
        Assert.Equal("model.Value", entry.Key);
        Assert.Equal("Invalid case.", Assert.Single(entry.Value.Errors).ErrorMessage);
    }

    [Fact]
    public void Validate_ClassLevelSuppressedExplicitUnion_DoesNotInvokeGetter()
    {
        using var services = CreateServices();
        var context = CreateContext();
        context.ModelState.SetModelValue("model.Value.Name", "ignored", "ignored");

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", new SuppressedExplicitUnion());

        Assert.True(context.ModelState.IsValid);
        Assert.Equal(ModelValidationState.Skipped, context.ModelState["model.Value.Name"].ValidationState);
    }

    [Fact]
    public void Validate_ValidationStateStrategy_PreservesDeclaredChildMetadata()
    {
        using var services = CreateServices();
        var context = CreateContext();
        var value = new ValidatableCase("invalid");
        object model = new ValidatableUnion(value);
        var metadata = services.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(typeof(object));
        var strategy = new Mock<IValidationStrategy>();
        strategy.Setup(s => s.GetChildren(It.IsAny<ModelMetadata>(), "model", model))
            .Returns(() => new[] { new ValidationEntry(metadata, "model.Value", value) }.AsEnumerable().GetEnumerator());
        var validationState = new ValidationStateDictionary
        {
            [model] = new ValidationStateEntry { Strategy = strategy.Object },
        };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, validationState, "model", model);

        Assert.True(context.ModelState.IsValid);
        Assert.Empty(context.ModelState);
        strategy.Verify(s => s.GetChildren(It.IsAny<ModelMetadata>(), "model", model), Times.Once);
    }

    [Fact]
    public void Validate_UnionCycle_DoesNotRevisitCase()
    {
        using var services = CreateServices();
        var context = CreateContext();
        var value = new RecursiveCase();
        value.Child = new RecursiveUnion(value);

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", value.Child);

        Assert.False(context.ModelState.IsValid);
        var entry = Assert.Single(context.ModelState);
        Assert.Equal("model.Value.Name", entry.Key);
        Assert.Equal("The Name field is required.", Assert.Single(entry.Value.Errors).ErrorMessage);
    }

    [Fact]
    public void Validate_UnionTraversal_RespectsMaxValidationDepth()
    {
        using var services = CreateServices(options => options.MaxValidationDepth = 1);
        var context = CreateContext();
        var model = new ValidatableUnion(new ValidatableCase("valid"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model));

        Assert.Contains("MaxValidationDepth", exception.Message);
    }

    [Fact]
    public void Validate_OrdinaryObjectProperty_DoesNotUseRuntimeTypeMetadata()
    {
        using var services = CreateServices();
        var context = CreateContext();
        var model = new ObjectEnvelope { Value = new ValidatableCase("invalid") };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.True(context.ModelState.IsValid);
        Assert.Empty(context.ModelState);
    }

    [Fact]
    public void Validate_UnionTraversal_RespectsMaxModelStateErrors()
    {
        using var services = CreateServices();
        var context = CreateContext();
        context.ModelState.MaxAllowedErrors = 1;
        var model = new[]
        {
            new ValidatableUnion(new ValidatableCase("invalid")),
            new ValidatableUnion(new ValidatableCase("invalid")),
        };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.False(context.ModelState.IsValid);
        Assert.True(context.ModelState.HasReachedMaxErrors);
        Assert.Equal(1, context.ModelState.ErrorCount);
    }

    [Fact]
    public void Validate_UnionWithOtherProperties_PreservesPropertyValidationAndFilters()
    {
        using var services = CreateServices();
        var context = CreateContext();
        var model = new CustomUnion { Value = new ValidatableCase("invalid") };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.False(context.ModelState.IsValid);
        var entry = Assert.Single(context.ModelState);
        Assert.Equal("model.Name", entry.Key);
        Assert.Equal("The Name field is required.", Assert.Single(entry.Value.Errors).ErrorMessage);
    }

    [Fact]
    public void Validate_UnionWithOtherObjectProperty_DoesNotUseRuntimeTypeMetadata()
    {
        using var services = CreateServices();
        var context = CreateContext();
        var model = new UnionWithOtherObjectProperty { Other = new ValidatableCase("invalid") };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.True(context.ModelState.IsValid);
        Assert.Empty(context.ModelState);
    }

    [Theory]
    [InlineData(null, "valid", "The Value field is required.")]
    [InlineData("public", "invalid", "Invalid case.")]
    public void Validate_ExplicitUnionValueWithPublicNamesake_ValidatesBothValues(string publicValue, string caseName, string errorMessage)
    {
        using var services = CreateServices();
        var context = CreateContext();
        var model = new UnionWithPublicNamesake(publicValue, new ValidatableCase(caseName));

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.False(context.ModelState.IsValid);
        var entry = Assert.Single(context.ModelState);
        Assert.Equal("model.Value", entry.Key);
        Assert.Equal(errorMessage, Assert.Single(entry.Value.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_SuppressedValue_DoesNotInvokeGetter(bool isUnion)
    {
        using var services = CreateServices();
        var context = CreateContext();
        object model = isUnion ? new SuppressedGetterUnion() : new SuppressedGetterModel();
        context.ModelState.SetModelValue("model.Value", "ignored", "ignored");

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.True(context.ModelState.IsValid);
        Assert.Equal(ModelValidationState.Skipped, context.ModelState["model.Value"].ValidationState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_UnionValue_EvaluatesFilterOnceBeforeGetter(bool suppressValue)
    {
        using var services = CreateServices();
        var context = CreateContext();
        var model = new FilterTrackingUnion { SuppressValue = suppressValue };

        services.GetRequiredService<IObjectModelValidator>().Validate(context, null, "model", model);

        Assert.Equal(1, model.FilterCalls);
        Assert.Equal(suppressValue ? 0 : 1, model.GetterCalls);
        Assert.Equal(suppressValue, context.ModelState.IsValid);
        Assert.Equal(suppressValue ? 0 : 1, context.ModelState.ErrorCount);
    }

    private static ServiceProvider CreateServices(Action<MvcOptions> configure = null, bool decorateMetadataProvider = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers(options => configure?.Invoke(options));
        if (decorateMetadataProvider)
        {
            services.AddSingleton<IModelMetadataProvider>(provider =>
                new MetadataDecorator(ActivatorUtilities.CreateInstance<DefaultModelMetadataProvider>(provider)));
        }

        return services.BuildServiceProvider();
    }

    private static ActionContext CreateContext() => new()
    {
        HttpContext = new DefaultHttpContext(),
    };

    public record ValidatableCase(string Name) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Name == "invalid")
            {
                yield return new ValidationResult("Invalid case.");
            }
        }
    }

    public union ValidatableUnion(ValidatableCase, int);
    public union RecursiveUnion(RecursiveCase, int);

    public class RecursiveCase
    {
        [Required]
        public string Name { get; set; }

        public RecursiveUnion Child { get; set; }
    }

    private class ObjectEnvelope
    {
        public object Value { get; set; }
    }

    private class CustomUnion : IUnion
    {
        [Required]
        public string Name { get; set; }

        [ValidateNever]
        public object Value { get; set; }
    }

    private class ExplicitUnion(object value) : IUnion
    {
        object IUnion.Value => value;
    }

    private sealed class MetadataDecorator(IModelMetadataProvider inner) : IModelMetadataProvider
    {
        public ModelMetadata GetMetadataForType(Type modelType) => inner.GetMetadataForType(modelType);

        public IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType) => inner.GetMetadataForProperties(modelType);
    }

    private class LengthUnion(string value) : IUnion
    {
        [MaxLength(3)]
        public object Value => value;
    }

    [ValidateNever]
    private class SuppressedExplicitUnion : IUnion
    {
        object IUnion.Value => throw new InvalidOperationException("Suppressed getter was invoked.");
    }

    private class UnionWithOtherObjectProperty : IUnion
    {
        public object Value => 42;

        public object Other { get; set; }
    }

    private class UnionWithPublicNamesake(object publicValue, object caseValue) : IUnion
    {
        [Required]
        public object Value => publicValue;

        object IUnion.Value => caseValue;
    }

    private class SuppressedGetterModel
    {
        [Required]
        public string Name { get; set; } = "valid";

        [ValidateNever]
        public object Value => throw new InvalidOperationException("Suppressed getter was invoked.");
    }

    private class SuppressedGetterUnion : SuppressedGetterModel, IUnion
    {
    }

    private class FilterTrackingUnion : IUnion
    {
        public bool SuppressValue { get; set; }

        public int FilterCalls { get; set; }

        public int GetterCalls { get; private set; }

        [TrackingValidationFilter]
        public object Value
        {
            get
            {
                GetterCalls++;
                return new ValidatableCase("invalid");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    private sealed class TrackingValidationFilterAttribute : Attribute, IPropertyValidationFilter
    {
        public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry)
        {
            var model = (FilterTrackingUnion)parentEntry.Model;
            model.FilterCalls++;
            return !model.SuppressValue;
        }
    }
}
