// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class ValidatableObjectAdapterTest
{
    private static readonly ModelMetadataProvider _metadataProvider
        = TestModelMetadataProvider.CreateDefaultProvider();

    // Inspired by DataAnnotationsModelValidatorTest.Validate_SetsMemberName_AsExpectedData but using a type that
    // implements IValidatableObject. Values are metadata, expected DisplayName, and expected MemberName.
    public static TheoryData<ModelMetadata, string, string> Validate_PassesExpectedNamesData
    {
        get
        {
            var method = typeof(SampleModel).GetMethod(nameof(SampleModel.IsLovely));
            var parameter = method.GetParameters()[0]; // IsLovely(SampleModel other)
            return new TheoryData<ModelMetadata, string, string>
                {
                    {
                        // Validating a property.
                        _metadataProvider.GetMetadataForProperty(
                            typeof(SampleModelContainer),
                            nameof(SampleModelContainer.SampleModel)),
                        nameof(SampleModelContainer.SampleModel),
                        nameof(SampleModelContainer.SampleModel)
                    },
                    {
                        // Validating a property with [Display(Name = "...")].
                        _metadataProvider.GetMetadataForProperty(
                            typeof(SampleModelContainer),
                            nameof(SampleModelContainer.SampleModelWithDisplay)),
                        "sample model",
                        nameof(SampleModelContainer.SampleModelWithDisplay)
                    },
                    {
                        // Validating a parameter.
                        _metadataProvider.GetMetadataForParameter(parameter),
                        "other",
                        "other"
                    },
                    {
                        // Validating a top-level parameter when using old-fashioned metadata provider.
                        // Or, validating an element of a collection.
                        _metadataProvider.GetMetadataForType(typeof(SampleModel)),
                        nameof(SampleModel),
                        null
                    },
                };
        }
    }

    public static TheoryData<ValidationResult[], ModelValidationResult[]> Validate_ReturnsExpectedResultsData
    {
        get
        {
            return new TheoryData<ValidationResult[], ModelValidationResult[]>
                {
                    {
                        new[] { new ValidationResult("Error message") },
                        new[] { new ModelValidationResult(memberName: null, message: "Error message") }
                    },
                    {
                        new[] { new ValidationResult("Error message", new[] { nameof(SampleModel.FirstName) }) },
                        new[] { new ModelValidationResult(nameof(SampleModel.FirstName), "Error message") }
                    },
                    {
                        new[]
                        {
                            new ValidationResult("Error message1"),
                            new ValidationResult("Error message2", new[] { nameof(SampleModel.FirstName) }),
                            new ValidationResult("Error message3", new[] { nameof(SampleModel.LastName) }),
                            new ValidationResult("Error message4", new[] { nameof(SampleModel) }),
                        },
                        new[]
                        {
                            new ModelValidationResult(memberName: null, message: "Error message1"),
                            new ModelValidationResult(nameof(SampleModel.FirstName), "Error message2"),
                            new ModelValidationResult(nameof(SampleModel.LastName), "Error message3"),
                            // No special case for ValidationContext.MemberName==ValidationResult.MemberName
                            new ModelValidationResult(nameof(SampleModel), "Error message4"),
                        }
                    },
                    {
                        new[]
                        {
                            new ValidationResult("Error message1", new[]
                            {
                                nameof(SampleModel.FirstName),
                                nameof(SampleModel.LastName),
                            }),
                            new ValidationResult("Error message2"),
                        },
                        new[]
                        {
                            new ModelValidationResult(nameof(SampleModel.FirstName), "Error message1"),
                            new ModelValidationResult(nameof(SampleModel.LastName), "Error message1"),
                            new ModelValidationResult(memberName: null, message: "Error message2"),
                        }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(Validate_PassesExpectedNamesData))]
    public void Validate_PassesExpectedNames(
        ModelMetadata metadata,
        string expectedDisplayName,
        string expectedMemberName)
    {
        // Arrange
        var adapter = new ValidatableObjectAdapter();
        var model = new SampleModel();
        var validationContext = new ModelValidationContext(
            new ActionContext(),
            metadata,
            _metadataProvider,
            container: new SampleModelContainer(),
            model: model);

        // Act
        var results = adapter.Validate(validationContext);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);

        Assert.Equal(expectedDisplayName, model.DisplayName);
        Assert.Equal(expectedMemberName, model.MemberName);
        Assert.Equal(model, model.ObjectInstance);
    }

    [Theory]
    [MemberData(nameof(Validate_ReturnsExpectedResultsData))]
    public void Validate_ReturnsExpectedResults(
        ValidationResult[] innerResults,
        ModelValidationResult[] expectedResults)
    {
        // Arrange
        var adapter = new ValidatableObjectAdapter();
        var model = new SampleModel();
        foreach (var result in innerResults)
        {
            model.ValidationResults.Add(result);
        }

        var metadata = _metadataProvider.GetMetadataForProperty(
            typeof(SampleModelContainer),
            nameof(SampleModelContainer.SampleModel));
        var validationContext = new ModelValidationContext(
            new ActionContext(),
            metadata,
            _metadataProvider,
            container: null,
            model: model);

        // Act
        var results = adapter.Validate(validationContext);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedResults, results, ModelValidationResultComparer.Instance);
    }

    private class SampleModel : IValidatableObject
    {
        // "Real" properties.

        public string FirstName { get; set; }

        public string LastName { get; set; }

        // ValidationContext members passed to Validate(...)

        public string DisplayName { get; private set; }

        public string MemberName { get; private set; }

        public object ObjectInstance { get; private set; }

        // What Validate(...) should return.

        public IList<ValidationResult> ValidationResults { get; } = new List<ValidationResult>();

        // Test method.

        public bool IsLovely(SampleModel other)
        {
            return true;
        }

        // IValidatableObject for realz.

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DisplayName = validationContext.DisplayName;
            MemberName = validationContext.MemberName;
            ObjectInstance = validationContext.ObjectInstance;

            return ValidationResults;
        }
    }

    private class SampleModelContainer
    {
        [Display(Name = "sample model")]
        public SampleModel SampleModelWithDisplay { get; set; }

        public SampleModel SampleModel { get; set; }
    }
}
