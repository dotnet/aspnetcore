// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Microbenchmarks;

public class ValidatableTypeInfoBenchmark
{
    private IValidatableInfo _simpleTypeInfo = null!;
    private IValidatableInfo _complexTypeInfo = null!;
    private IValidatableInfo _hierarchicalTypeInfo = null!;
    private IValidatableInfo _ivalidatableObjectTypeInfo = null!;

    private ValidateContext _context = null!;
    private SimpleModel _simpleModel = null!;
    private ComplexModel _complexModel = null!;
    private HierarchicalModel _hierarchicalModel = null!;
    private ValidatableObjectModel _validatableObjectModel = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        var mockResolver = new MockValidatableTypeInfoResolver();

        services.AddValidation(options =>
        {
            // Register our mock resolver
            options.Resolvers.Insert(0, mockResolver);
        });

        var serviceProvider = services.BuildServiceProvider();
        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        _context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(new object(), serviceProvider, null),
            ValidationErrors = new Dictionary<string, string[]>(StringComparer.Ordinal)
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

        // Get the type info instances from validation options using the mock resolver
        validationOptions.TryGetValidatableTypeInfo(typeof(SimpleModel), out _simpleTypeInfo);
        validationOptions.TryGetValidatableTypeInfo(typeof(ComplexModel), out _complexTypeInfo);
        validationOptions.TryGetValidatableTypeInfo(typeof(HierarchicalModel), out _hierarchicalTypeInfo);
        validationOptions.TryGetValidatableTypeInfo(typeof(ValidatableObjectModel), out _ivalidatableObjectTypeInfo);

        // Ensure we have all type infos (this should not be needed with our mock resolver)
        if (_simpleTypeInfo == null || _complexTypeInfo == null ||
            _hierarchicalTypeInfo == null || _ivalidatableObjectTypeInfo == null)
        {
            throw new InvalidOperationException("Failed to register one or more type infos with mock resolver");
        }
    }

    [Benchmark(Description = "Validate Simple Model")]
    [BenchmarkCategory("Simple")]
    public async Task ValidateSimpleModel()
    {
        _context.ValidationErrors.Clear();
        await _simpleTypeInfo.ValidateAsync(_simpleModel, _context, default);
    }

    [Benchmark(Description = "Validate Complex Model")]
    [BenchmarkCategory("Complex")]
    public async Task ValidateComplexModel()
    {
        _context.ValidationErrors.Clear();
        await _complexTypeInfo.ValidateAsync(_complexModel, _context, default);
    }

    [Benchmark(Description = "Validate Hierarchical Model")]
    [BenchmarkCategory("Hierarchical")]
    public async Task ValidateHierarchicalModel()
    {
        _context.ValidationErrors.Clear();
        await _hierarchicalTypeInfo.ValidateAsync(_hierarchicalModel, _context, default);
    }

    [Benchmark(Description = "Validate IValidatableObject Model")]
    [BenchmarkCategory("IValidatableObject")]
    public async Task ValidateIValidatableObjectModel()
    {
        _context.ValidationErrors.Clear();
        await _ivalidatableObjectTypeInfo.ValidateAsync(_validatableObjectModel, _context, default);
    }

    [Benchmark(Description = "Validate invalid Simple Model")]
    [BenchmarkCategory("Invalid")]
    public async Task ValidateInvalidSimpleModel()
    {
        _context.ValidationErrors.Clear();
        _simpleModel.Email = "invalid-email";
        await _simpleTypeInfo.ValidateAsync(_simpleModel, _context, default);
    }

    [Benchmark(Description = "Validate invalid IValidatableObject Model")]
    [BenchmarkCategory("Invalid")]
    public async Task ValidateInvalidIValidatableObjectModel()
    {
        _context.ValidationErrors.Clear();
        _validatableObjectModel.CustomField = "Invalid";
        await _ivalidatableObjectTypeInfo.ValidateAsync(_validatableObjectModel, _context, default);
    }

    #region Helper methods to create type info instances manually if needed

    private ValidatablePropertyInfo CreatePropertyInfo(string name, Type type, params ValidationAttribute[] attributes)
    {
        return new MockValidatablePropertyInfo(
            typeof(SimpleModel),
            type,
            name,
            name,
            attributes);
    }

    #endregion

    #region Test Models

    public class SimpleModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }
    }

    public class ComplexModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public List<string> Items { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    public class ChildModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int ParentId { get; set; }
    }

    public class HierarchicalModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ChildModel Child { get; set; }

        public List<SimpleModel> Siblings { get; set; }
    }

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

    #region Mock Implementations for Testing

    private class MockValidatableTypeInfo(Type type, ValidatablePropertyInfo[] members) : ValidatableTypeInfo(type, members)
    {
    }

    private class MockValidatablePropertyInfo(
        Type containingType,
        Type propertyType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes) : ValidatablePropertyInfo(containingType, propertyType, name, displayName)
    {
        private readonly ValidationAttribute[] _validationAttributes = validationAttributes;

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    #endregion

    #region Mock Resolver Implementation

    private class MockValidatableTypeInfoResolver : IValidatableInfoResolver
    {
        private readonly Dictionary<Type, ValidatableTypeInfo> _typeInfoCache = [];

        public MockValidatableTypeInfoResolver()
        {
            // Initialize the cache with our test models
            _typeInfoCache[typeof(SimpleModel)] = CreateSimpleModelTypeInfo();
            _typeInfoCache[typeof(ComplexModel)] = CreateComplexModelTypeInfo();
            _typeInfoCache[typeof(HierarchicalModel)] = CreateHierarchicalModelTypeInfo();
            _typeInfoCache[typeof(ValidatableObjectModel)] = CreateValidatableObjectModelTypeInfo();

            // Add child models that might be validated separately
            _typeInfoCache[typeof(ChildModel)] = CreateChildModelTypeInfo();
        }

        private ValidatableTypeInfo CreateSimpleModelTypeInfo()
        {
            return new MockValidatableTypeInfo(
                typeof(SimpleModel),
                [
                    CreatePropertyInfo(typeof(SimpleModel), "Id", typeof(int)),
                    CreatePropertyInfo(typeof(SimpleModel), "Name", typeof(string)),
                    CreatePropertyInfo(typeof(SimpleModel), "Email", typeof(string), new EmailAddressAttribute())
                ]);
        }

        private ValidatableTypeInfo CreateComplexModelTypeInfo()
        {
            return new MockValidatableTypeInfo(
                typeof(ComplexModel),
                [
                    CreatePropertyInfo(typeof(ComplexModel), "Id", typeof(int)),
                    CreatePropertyInfo(typeof(ComplexModel), "Name", typeof(string)),
                    CreatePropertyInfo(typeof(ComplexModel), "Properties", typeof(Dictionary<string, string>)),
                    CreatePropertyInfo(typeof(ComplexModel), "Items", typeof(List<string>)),
                    CreatePropertyInfo(typeof(ComplexModel), "CreatedOn", typeof(DateTime))
                ]);
        }

        private ValidatableTypeInfo CreateChildModelTypeInfo()
        {
            return new MockValidatableTypeInfo(
                typeof(ChildModel),
                [
                    CreatePropertyInfo(typeof(ChildModel), "Id", typeof(int)),
                    CreatePropertyInfo(typeof(ChildModel), "Name", typeof(string)),
                    CreatePropertyInfo(typeof(ChildModel), "ParentId", typeof(int))
                ]);
        }

        private ValidatableTypeInfo CreateHierarchicalModelTypeInfo()
        {
            return new MockValidatableTypeInfo(
                typeof(HierarchicalModel),
                [
                    CreatePropertyInfo(typeof(HierarchicalModel), "Id", typeof(int)),
                    CreatePropertyInfo(typeof(HierarchicalModel), "Name", typeof(string)),
                    CreatePropertyInfo(typeof(HierarchicalModel), "Child", typeof(ChildModel)),
                    CreatePropertyInfo(typeof(HierarchicalModel), "Siblings", typeof(List<SimpleModel>))
                ]);
        }

        private ValidatableTypeInfo CreateValidatableObjectModelTypeInfo()
        {
            return new MockValidatableTypeInfo(
                typeof(ValidatableObjectModel),
                [
                    CreatePropertyInfo(typeof(ValidatableObjectModel), "Id", typeof(int)),
                    CreatePropertyInfo(typeof(ValidatableObjectModel), "Name", typeof(string)),
                    CreatePropertyInfo(typeof(ValidatableObjectModel), "CustomField", typeof(string))
                ]);
        }

        private ValidatablePropertyInfo CreatePropertyInfo(Type containingType, string name, Type type, params ValidationAttribute[] attributes)
        {
            return new MockValidatablePropertyInfo(
                containingType,
                type,
                name,
                name, // Use name as display name
                attributes);
        }

        public bool TryGetValidatableTypeInfo(Type type, out IValidatableInfo validatableInfo)
        {
            if (_typeInfoCache.TryGetValue(type, out var typeInfo))
            {
                validatableInfo = typeInfo;
                return true;
            }
            validatableInfo = null;
            return false;
        }

        public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, out IValidatableInfo validatableInfo)
        {
            validatableInfo = null;
            return false;
        }
    }
    #endregion
}
