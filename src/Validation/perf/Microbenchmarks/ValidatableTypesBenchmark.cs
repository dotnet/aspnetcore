#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.Extensions.Validation.Microbenchmarks;

public class ValidatableTypeInfoBenchmark
{
    private IValidatableTypeInfo _simpleTypeInfo = null!;
    private IValidatableTypeInfo _complexTypeInfo = null!;
    private IValidatableTypeInfo _hierarchicalTypeInfo = null!;
    private IValidatableTypeInfo _ivalidatableObjectTypeInfo = null!;

    private ValidateContext _context = null!;
    private IServiceProvider _serviceProvider = null!;
    private ValidationOptions _validationOptions = null!;
    private SimpleModel _simpleModel = null!;
    private ComplexModel _complexModel = null!;
    private HierarchicalModel _hierarchicalModel = null!;
    private ValidatableObjectModel _validatableObjectModel = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // The ValidationsGenerator intercepts this AddValidation() call and registers the
        // generated resolver that knows how to validate the [ValidatableType] models below.
        services.AddValidation();

        var serviceProvider = services.BuildServiceProvider();
        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        _serviceProvider = serviceProvider;
        _validationOptions = validationOptions;

        _context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ServiceProvider = serviceProvider,
        };

        // Create the model instances
        _simpleModel = new SimpleModel
        {
            Id = 1,
            Name = "Test Name",
            Email = "test@example.com"
        };

        _complexModel = new ComplexModel
        {
            Id = 1,
            Name = "Complex Model",
            Properties = new Dictionary<string, string>
            {
                ["Prop1"] = "Value1",
                ["Prop2"] = "Value2"
            },
            Items = ["Item1", "Item2", "Item3"],
            CreatedOn = DateTime.UtcNow
        };

        _hierarchicalModel = new HierarchicalModel
        {
            Id = 1,
            Name = "Parent Model",
            Child = new ChildModel
            {
                Id = 2,
                Name = "Child Model",
                ParentId = 1
            },
            Siblings =
            [
                new SimpleModel { Id = 3, Name = "Sibling 1", Email = "sibling1@example.com" },
                new SimpleModel { Id = 4, Name = "Sibling 2", Email = "sibling2@example.com" }
            ]
        };

        _validatableObjectModel = new ValidatableObjectModel
        {
            Id = 1,
            Name = "Validatable Model",
            CustomField = "Valid Value"
        };

        // Get the type info instances from validation options using the generated resolver
        validationOptions.TryGetValidatableTypeInfo(typeof(SimpleModel), out _simpleTypeInfo);
        validationOptions.TryGetValidatableTypeInfo(typeof(ComplexModel), out _complexTypeInfo);
        validationOptions.TryGetValidatableTypeInfo(typeof(HierarchicalModel), out _hierarchicalTypeInfo);
        validationOptions.TryGetValidatableTypeInfo(typeof(ValidatableObjectModel), out _ivalidatableObjectTypeInfo);

        // Ensure we have all type infos
        if (_simpleTypeInfo == null || _complexTypeInfo == null ||
            _hierarchicalTypeInfo == null || _ivalidatableObjectTypeInfo == null)
        {
            throw new InvalidOperationException("Failed to resolve one or more type infos from the generated resolver");
        }
    }

    [Benchmark(Description = "Validate Simple Model")]
    [BenchmarkCategory("Simple")]
    public async Task ValidateSimpleModel()
    {
        ClearValidationErrors();
        await _simpleTypeInfo.ValidateAsync(_simpleModel, _context, default);
    }

    [Benchmark(Description = "Validate Complex Model")]
    [BenchmarkCategory("Complex")]
    public async Task ValidateComplexModel()
    {
        ClearValidationErrors();
        await _complexTypeInfo.ValidateAsync(_complexModel, _context, default);
    }

    [Benchmark(Description = "Validate Hierarchical Model")]
    [BenchmarkCategory("Hierarchical")]
    public async Task ValidateHierarchicalModel()
    {
        ClearValidationErrors();
        await _hierarchicalTypeInfo.ValidateAsync(_hierarchicalModel, _context, default);
    }

    [Benchmark(Description = "Validate IValidatableObject Model")]
    [BenchmarkCategory("IValidatableObject")]
    public async Task ValidateIValidatableObjectModel()
    {
        ClearValidationErrors();
        await _ivalidatableObjectTypeInfo.ValidateAsync(_validatableObjectModel, _context, default);
    }

    [Benchmark(Description = "Validate invalid Simple Model")]
    [BenchmarkCategory("Invalid")]
    public async Task ValidateInvalidSimpleModel()
    {
        ClearValidationErrors();
        _simpleModel.Email = "invalid-email";
        await _simpleTypeInfo.ValidateAsync(_simpleModel, _context, default);
    }

    [Benchmark(Description = "Validate invalid IValidatableObject Model")]
    [BenchmarkCategory("Invalid")]
    public async Task ValidateInvalidIValidatableObjectModel()
    {
        ClearValidationErrors();
        _validatableObjectModel.CustomField = "Invalid";
        await _ivalidatableObjectTypeInfo.ValidateAsync(_validatableObjectModel, _context, default);
    }

    private void ClearValidationErrors()
    {
        // ValidationErrors is read-only; recreate the context to reset accumulated errors.
        _context = new ValidateContext
        {
            ValidationOptions = _validationOptions,
            ServiceProvider = _serviceProvider,
        };
    }

    #region Test Models

    [ValidatableType]
    public class SimpleModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }
    }

    [ValidatableType]
    public class ComplexModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public List<string> Items { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    [ValidatableType]
    public class ChildModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int ParentId { get; set; }
    }

    [ValidatableType]
    public class HierarchicalModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ChildModel Child { get; set; }

        public List<SimpleModel> Siblings { get; set; }
    }

    [ValidatableType]
    public class ValidatableObjectModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string CustomField { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CustomField == "Invalid")
            {
                yield return new ValidationResult("CustomField has an invalid value", new[] { nameof(CustomField) });
            }
        }
    }

    #endregion
}
