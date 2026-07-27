// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Tests;

public class ValidatableTypeInfoTests : ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_ValidatesComplexType_WithNestedProperties(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedPerson>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);
        var person = new GeneratedPerson { Age = 150, Address = new GeneratedAddress() };

        await ValidateAsync(typeInfo, person, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
        Assert.Equal("The field Age must be between 0 and 120.", context.ValidationErrors["Age"].Select(e => e.ErrorMessage).Single());
        Assert.Equal("The Street field is required.", context.ValidationErrors["Address.Street"].Select(e => e.ErrorMessage).Single());
        Assert.Equal("The City field is required.", context.ValidationErrors["Address.City"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesIValidatableObject_Implementation(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedEmployee>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new GeneratedEmployee { Name = "John", Salary = -1 }, context, useAsync, default);

        var error = Assert.Single(context.ValidationErrors!);
        Assert.Equal("Salary", error.Key);
        Assert.Equal("Salary must be a positive value.", error.Value.Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesPolymorphicTypes_WithSubtypes(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedCar>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new GeneratedCar { Doors = 7 }, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The field Doors must be between 2 and 5.", context.ValidationErrors["Doors"].Select(e => e.ErrorMessage).Single());
        Assert.Equal("The Make field is required.", context.ValidationErrors["Make"].Select(e => e.ErrorMessage).Single());
        Assert.Equal("The Model field is required.", context.ValidationErrors["Model"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesCollections_OfValidatableTypes(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedOrder>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);
        var order = new GeneratedOrder
        {
            OrderNumber = "ORD-1",
            Items = [new() { ProductName = "Valid", Quantity = 5 }, new() { Quantity = 0 }, new() { ProductName = "Another", Quantity = 200 }]
        };

        await ValidateAsync(typeInfo, order, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Contains("Items[1].ProductName", context.ValidationErrors.Keys);
        Assert.Contains("Items[1].Quantity", context.ValidationErrors.Keys);
        Assert.Contains("Items[2].Quantity", context.ValidationErrors.Keys);
    }

    // Regression test for https://github.com/dotnet/aspnetcore/issues/61953
    // The validation walk over IEnumerable properties iterates a Dictionary<TKey, TValue> as a
    // sequence of KeyValuePair<TKey, TValue>. Dictionary values are validated; keys themselves are not validated.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_ValidatesDictionaryValues(bool useAsync)
    {
        // Arrange
        var itemType = new TestValidatableTypeInfo(
            typeof(OrderItem),
            [
                CreatePropertyInfo(typeof(OrderItem), typeof(string), "ProductName", "ProductName",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(OrderItem), typeof(int), "Quantity", "Quantity",
                    [new RangeAttribute(1, 100)])
            ]);

        var catalogType = new TestValidatableTypeInfo(
            typeof(Catalog),
            [
                CreatePropertyInfo(typeof(Catalog), typeof(Dictionary<string, OrderItem>), "Items", "Items",
                    [])
            ]);

        var catalog = new Catalog
        {
            Items =
            {
                ["first"] = new OrderItem { /* Missing ProductName (required) */ Quantity = 0 /* Invalid quantity */ },
                ["second"] = new OrderItem { ProductName = "Valid Product", Quantity = 5 }
            }
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(OrderItem), itemType },
                { typeof(Catalog), catalogType }
            }),
            ValidationContext = new ValidationContext(catalog)
        };

        // Act
        await ValidateAsync(catalogType, catalog, context, useAsync, default);

        // Assert
        // The invalid "first" dictionary value is validated. The valid "second" value produces no errors.
        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(2, context.ValidationErrors.Count);
        Assert.Equal("The ProductName field is required.",
            Assert.Contains("Items[first].ProductName", context.ValidationErrors).First());
        Assert.Equal("The field Quantity must be between 1 and 100.",
            Assert.Contains("Items[first].Quantity", context.ValidationErrors).First());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesNullValues_Appropriately(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedNullablePerson>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new GeneratedNullablePerson(), context, useAsync, default);

        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_RespectsMaxDepthOption_ForCircularReferences(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.MaxDepth = 3);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedTreeNode>(options);
        var root = new GeneratedTreeNode { Name = "Root" };
        var level1 = new GeneratedTreeNode { Name = "Level1", Parent = root };
        var level2 = new GeneratedTreeNode { Name = "Level2", Parent = level1 };
        var level3 = new GeneratedTreeNode { Name = "Level3", Parent = level2 };
        root.Children.Add(level1);
        level1.Children.Add(level2);
        level2.Children.Add(level3);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ValidateAsync(typeInfo, root, context, useAsync, default));

        Assert.Contains("Maximum validation depth of 3 exceeded", exception.Message);
        Assert.Equal(0, context.CurrentDepth);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesCustomValidationAttributes(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedProduct>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new GeneratedProduct { SKU = "INVALID" }, context, useAsync, default);

        Assert.Equal("SKU must start with 'PROD-'.", Assert.Single(context.ValidationErrors!).Value.Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesMultipleErrorsOnSameProperty(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedUser>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new GeneratedUser { Password = "abc" }, context, useAsync, default);

        var passwordErrors = context.ValidationErrors!["Password"].Select(e => e.ErrorMessage).ToArray();
        Assert.Contains("Password must be at least 8 characters.", passwordErrors);
        Assert.Contains("Password must contain at least one number and one special character.", passwordErrors);
    }

    [Fact]
    public void TryFindProperty_UsesGeneratedPublicSurface()
    {
        var (_, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GeneratedDerivedEntity>(options);

        Assert.True(typeInfo.TryFindProperty("Name", options, out var nameProperty));
        Assert.NotNull(nameProperty);
        Assert.True(typeInfo.TryFindProperty("CreatedAt", options, out var createdAtProperty));
        Assert.NotNull(createdAtProperty);
        Assert.True(typeInfo.TryFindProperty("Id", options, out var idProperty));
        Assert.NotNull(idProperty);
        Assert.False(typeInfo.TryFindProperty("Missing", options, out var missing));
        Assert.Null(missing);
    }
}

[ValidatableType]
public class GeneratedPerson
{
    [Required]
    public string? Name { get; set; }
    [Range(0, 120)]
    public int Age { get; set; }
    public GeneratedAddress? Address { get; set; }
}

[ValidatableType]
public class GeneratedAddress
{
    [Required]
    public string? Street { get; set; }
    [Required]
    public string? City { get; set; }
}

[ValidatableType]
public class GeneratedEmployee : IValidatableObject
{
    [Required]
    public string? Name { get; set; }
    public decimal Salary { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Salary < 0)
        {
<<<<<<< HEAD
            { typeof(Person), typeInfo },
        });

        Assert.False(typeInfo.TryFindProperty("NonExistent", options, out var validatablePropertyInfo));
        Assert.Null(validatablePropertyInfo);
    }

    [Fact]
    public void TryFindProperty_ReturnsMatchingProperty_WhenPresent()
    {
        var nameProperty = CreatePropertyInfo(typeof(Person), typeof(string), "Name", "Name", []);
        var typeInfo = new TestValidatableTypeInfo(typeof(Person), [nameProperty]);
        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(Person), typeInfo },
        });

        Assert.True(typeInfo.TryFindProperty("Name", options, out var retrievedNameProperty));
        Assert.NotNull(retrievedNameProperty);
        Assert.Same(nameProperty, retrievedNameProperty);
    }

    [Fact]
    public void TryFindProperty_ReturnsInheritedProperty_FromSuperType()
    {
        // BaseEntity declares Id; DerivedEntity declares Name. Looking up Id on the derived type
        // should resolve through the super-type info via the validation options resolver.
        var idProperty = CreatePropertyInfo(typeof(BaseEntity), typeof(Guid), "Id", "Id", []);
        var baseType = new TestValidatableTypeInfo(typeof(BaseEntity), [idProperty]);
        var nameProperty = CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", []);
        var derivedType = new TestValidatableTypeInfo(typeof(DerivedEntity), [nameProperty]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(BaseEntity), baseType },
            { typeof(DerivedEntity), derivedType },
        });

        Assert.True(derivedType.TryFindProperty("Name", options, out var localProperty));
        Assert.NotNull(localProperty);
        Assert.Same(nameProperty, localProperty);

        Assert.True(derivedType.TryFindProperty("Id", options, out var inheritedProperty));
        Assert.NotNull(inheritedProperty);
        Assert.Same(idProperty, inheritedProperty);

        Assert.False(derivedType.TryFindProperty("NonExistent", options, out var missingProperty));
        Assert.Null(missingProperty);
    }

    [Fact]
    public void TryFindProperty_LocalDeclarationShadowsInheritedProperty()
    {
        // If both base and derived declare a property with the same name, the derived (local)
        // declaration is returned, matching how ValidateAsync would visit derived members first.
        var baseNameProperty = CreatePropertyInfo(typeof(BaseEntity), typeof(string), "Name", "Name", []);
        var baseType = new TestValidatableTypeInfo(typeof(BaseEntity), [baseNameProperty]);
        var derivedNameProperty = CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", []);
        var derivedType = new TestValidatableTypeInfo(typeof(DerivedEntity), [derivedNameProperty]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(BaseEntity), baseType },
            { typeof(DerivedEntity), derivedType },
        });

        Assert.True(derivedType.TryFindProperty("Name", options, out var propertyInfo));
        Assert.NotNull(propertyInfo);
        Assert.Same(derivedNameProperty, propertyInfo);
    }

    [Fact]
    public void TryFindProperty_ReturnsFalseForInheritedMember_WhenSuperTypeNotResolvable()
    {
        // Only the derived type is registered. Local lookup still works; inherited members
        // remain unresolved and the method returns false without throwing.
        var nameProperty = CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", []);
        var derivedType = new TestValidatableTypeInfo(typeof(DerivedEntity), [nameProperty]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(DerivedEntity), derivedType },
        });

        Assert.True(derivedType.TryFindProperty("Name", options, out var localProperty));
        Assert.NotNull(localProperty);
        Assert.Same(nameProperty, localProperty);

        Assert.False(derivedType.TryFindProperty("Id", options, out var inheritedProperty));
        Assert.Null(inheritedProperty);
    }

    [Fact]
    public void TryFindProperty_WalksMultipleInheritanceLevels()
    {
        // Three-level chain: BaseEntity (Id) <- IntermediateEntity (CreatedAt) <- DerivedEntity (Name).
        // A lookup on DerivedEntity must reach members declared at every level of the chain.
        var idProperty = CreatePropertyInfo(typeof(BaseEntity), typeof(Guid), "Id", "Id", []);
        var createdAtProperty = CreatePropertyInfo(typeof(IntermediateEntity), typeof(DateTime), "CreatedAt", "CreatedAt", []);
        var nameProperty = CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", []);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(BaseEntity), new TestValidatableTypeInfo(typeof(BaseEntity), [idProperty]) },
            { typeof(IntermediateEntity), new TestValidatableTypeInfo(typeof(IntermediateEntity), [createdAtProperty]) },
            { typeof(DerivedEntity), new TestValidatableTypeInfo(typeof(DerivedEntity), [nameProperty]) },
        });

        Assert.True(options.TryGetValidatableTypeInfo(typeof(DerivedEntity), out var derivedEntityInfo));

        Assert.True(derivedEntityInfo.TryFindProperty("Name", options, out var fromDerived));
        Assert.NotNull(fromDerived);
        Assert.Same(nameProperty, fromDerived);

        Assert.True(derivedEntityInfo.TryFindProperty("CreatedAt", options, out var fromIntermediate));
        Assert.NotNull(fromIntermediate);
        Assert.Same(createdAtProperty, fromIntermediate);

        Assert.True(derivedEntityInfo.TryFindProperty("Id", options, out var fromBase));
        Assert.NotNull(fromBase);
        Assert.Same(idProperty, fromBase);
    }

    [Fact]
    public void TryFindProperty_ResolvesInterfaceDeclaredProperty()
    {
        // Property declared on an interface implemented by the target type. ValidatableTypeInfo's
        // _superTypes list is populated by GetAllImplementedTypes(), which includes interfaces.
        var auditedProperty = CreatePropertyInfo(typeof(IAuditable), typeof(DateTime), "CreatedAt", "CreatedAt", []);
        var auditableTypeInfo = new TestValidatableTypeInfo(typeof(IAuditable), [auditedProperty]);
        var nameProperty = CreatePropertyInfo(typeof(AuditableThing), typeof(string), "Name", "Name", []);
        var thingTypeInfo = new TestValidatableTypeInfo(typeof(AuditableThing), [nameProperty]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(IAuditable), auditableTypeInfo },
            { typeof(AuditableThing), thingTypeInfo },
        });

        Assert.True(thingTypeInfo.TryFindProperty("CreatedAt", options, out var resolved));
        Assert.NotNull(resolved);
        Assert.Same(auditedProperty, resolved);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HiddenPropertyOnDerivedType_UsesDeclaredProperty(bool useAsync)
    {
        var queryOptions = new DerivedQueryOptions
        {
            IfMatch = "etag",
        };
        var propertyInfo = CreatePropertyInfo(typeof(DerivedQueryOptions), typeof(string), nameof(DerivedQueryOptions.IfMatch), nameof(DerivedQueryOptions.IfMatch), []);
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions([]),
            ValidationContext = new ValidationContext(queryOptions),
        };

        await ValidateAsync(propertyInfo, queryOptions, context, useAsync, default);

        Assert.Null(context.ValidationErrors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HiddenGenericPropertyOnDerivedType_UsesDeclaredProperty(bool useAsync)
    {
        var queryOptions = new GenericDerivedQueryOptions<int>
        {
            IfMatch = new GenericETag<int>(),
        };
        var propertyName = nameof(GenericDerivedQueryOptions<int>.IfMatch);
        var propertyInfo = CreatePropertyInfo(typeof(GenericDerivedQueryOptions<int>), typeof(GenericETag<int>), propertyName, propertyName, []);
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions([]),
            ValidationContext = new ValidationContext(queryOptions),
        };

        await ValidateAsync(propertyInfo, queryOptions, context, useAsync, default);

        Assert.Null(context.ValidationErrors);
    }

    private interface IAuditable
    {
        DateTime CreatedAt { get; }
    }

    private class AuditableThing : IAuditable
    {
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class QueryOptions
    {
        public object? IfMatch { get; set; }
    }

    private class DerivedQueryOptions : QueryOptions
    {
        public new string? IfMatch { get; set; }
    }

    private class GenericETag
    {
    }

    private class GenericETag<T> : GenericETag
    {
    }

    private class GenericQueryOptions
    {
        public virtual GenericETag? IfMatch { get; set; }
    }

    private class GenericDerivedQueryOptions<T> : GenericQueryOptions
    {
        public new GenericETag<T>? IfMatch
        {
            get => base.IfMatch as GenericETag<T>;
            set => base.IfMatch = value;
        }
    }

    // Returns no member names to validate https://github.com/dotnet/aspnetcore/issues/61739
    private class GlobalErrorObject : IValidatableObject
    {
        public int Data { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Data <= 0)
            {
                yield return new ValidationResult("Data must be positive.");
            }
        }
    }

    // Returns multiple member names to validate https://github.com/dotnet/aspnetcore/issues/61739
    private class MultiMemberErrorObject : IValidatableObject
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName))
            {
                // MULTIPLE member names
                yield return new ValidationResult(
                    "FirstName and LastName are required.",
                    [nameof(FirstName), nameof(LastName)]);
            }
        }
    }

    [CustomValidation]
    private class PropertyAndTypeLevelErrorObject : IValidatableObject
    {
        [Range(0, int.MaxValue, ErrorMessage = "Property attribute error")]
        public int Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value < 20)
            {
                yield return new ValidationResult($"IValidatableObject error");
            }
        }
    }

    private class CustomValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is PropertyAndTypeLevelErrorObject instance)
            {
                if (instance.Value < 10)
                {
                    return new ValidationResult($"Class attribute error");
                }
            }
            return ValidationResult.Success;
        }
    }

    private ValidatablePropertyInfo CreatePropertyInfo(
        Type containingType,
        Type propertyType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes)
    {
        return new TestValidatablePropertyInfo(
            containingType,
            propertyType,
            name,
            displayName,
            validationAttributes);
    }    // Test model classes
    private class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public Address? Address { get; set; }
    }

    private class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    private class Employee : IValidatableObject
    {
        public string? Name { get; set; }
        public string? Department { get; set; }
        public decimal Salary { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Salary < 0)
            {
                yield return new ValidationResult("Salary must be a positive value.", ["Salary"]);
            }
        }
    }

    private class Vehicle
    {
        public string? Make { get; set; }
        public string? Model { get; set; }
    }

    private class Car : Vehicle
    {
        public int Doors { get; set; }
    }

    private class Order
    {
        public string? OrderNumber { get; set; }
        public List<OrderItem> Items { get; set; } = [];
    }

    private class OrderItem
    {
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
    }

    private class Catalog
    {
        public Dictionary<string, OrderItem> Items { get; set; } = [];
    }

    private class TreeNode
    {
        public string Name { get; set; } = string.Empty;
        public TreeNode? Parent { get; set; }
        public List<TreeNode> Children { get; set; } = [];
    }

    private class Product
    {
        public string SKU { get; set; } = string.Empty;
    }

    private class User
    {
        public string? Password { get; set; } = string.Empty;
    }

    private class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    private class IntermediateEntity : BaseEntity
    {
        public DateTime CreatedAt { get; set; }
    }

    private class DerivedEntity : IntermediateEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private class PastDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime date && date > DateTime.Now)
            {
                return new ValidationResult("Date must be in the past.");
            }

            return ValidationResult.Success;
        }
    }

    private class CustomSkuValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string sku && !sku.StartsWith("PROD-", StringComparison.Ordinal))
            {
                return new ValidationResult("SKU must start with 'PROD-'.");
            }

            return ValidationResult.Success;
        }
    }

    private class PasswordComplexityAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string password)
            {
                var hasDigit = password.Any(c => char.IsDigit(c));
                var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

                if (!hasDigit || !hasSpecial)
                {
                    return new ValidationResult("Password must contain at least one number and one special character.");
                }
            }

            return ValidationResult.Success;
        }
    }

    // Test implementations
    private class TestValidatablePropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatablePropertyInfo(
            Type containingType,
            Type propertyType,
            string name,
            string displayName,
            ValidationAttribute[] validationAttributes)
            : base(containingType, propertyType, name, new TestLiteralDisplayName(displayName))
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidationOptions : ValidationOptions
    {
        public TestValidationOptions(Dictionary<Type, ValidatableTypeInfo> typeInfoMappings)
        {
            // Create a custom resolver that uses the dictionary
            var resolver = new DictionaryBasedResolver(typeInfoMappings);

            // Add it to the resolvers collection
            Resolvers.Add(resolver);
        }

        // Private resolver implementation that uses a dictionary lookup
        private class DictionaryBasedResolver : IValidatableInfoResolver
        {
            private readonly Dictionary<Type, ValidatableTypeInfo> _typeInfoMappings;

            public DictionaryBasedResolver(Dictionary<Type, ValidatableTypeInfo> typeInfoMappings)
            {
                _typeInfoMappings = typeInfoMappings;
            }

            public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableInfo)
            {
                if (_typeInfoMappings.TryGetValue(type, out var info))
                {
                    validatableInfo = info;
                    return true;
                }
                validatableInfo = null;
                return false;
            }

            public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableInfo)
            {
                validatableInfo = null;
                return false;
            }
=======
            yield return new ValidationResult("Salary must be a positive value.", [nameof(Salary)]);
>>>>>>> origin/main
        }
    }
}

[ValidatableType]
public class GeneratedVehicle
{
    [Required]
    public string? Make { get; set; }
    [Required]
    public string? Model { get; set; }
}

[ValidatableType]
public class GeneratedCar : GeneratedVehicle
{
    [Range(2, 5)]
    public int Doors { get; set; }
}

[ValidatableType]
public class GeneratedOrder
{
    [Required]
    public string? OrderNumber { get; set; }
    public List<GeneratedOrderItem> Items { get; set; } = [];
}

[ValidatableType]
public class GeneratedOrderItem
{
    [Required]
    public string? ProductName { get; set; }
    [Range(1, 100)]
    public int Quantity { get; set; }
}

[ValidatableType]
public class GeneratedNullablePerson
{
    public string? Name { get; set; }
    public GeneratedAddress? Address { get; set; }
}

[ValidatableType]
public class GeneratedTreeNode
{
    [Required]
    public string? Name { get; set; }
    public GeneratedTreeNode? Parent { get; set; }
    public List<GeneratedTreeNode> Children { get; set; } = [];
}

[ValidatableType]
public class GeneratedProduct
{
    [Required]
    [SkuValidation]
    public string SKU { get; set; } = string.Empty;
}

public sealed class SkuValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => value is string sku && !sku.StartsWith("PROD-", StringComparison.Ordinal) ? new ValidationResult("SKU must start with 'PROD-'.") : ValidationResult.Success;
}

[ValidatableType]
public class GeneratedUser
{
    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [PasswordComplexity]
    public string? Password { get; set; }
}

public sealed class PasswordComplexityAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => value is string password && (!password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            ? new ValidationResult("Password must contain at least one number and one special character.")
            : ValidationResult.Success;
}

[ValidatableType]
public class GeneratedBaseEntity
{
    [Required]
    public Guid Id { get; set; }
}

[ValidatableType]
public class GeneratedIntermediateEntity : GeneratedBaseEntity
{
    [Required]
    public DateTime CreatedAt { get; set; }
}

[ValidatableType]
public class GeneratedDerivedEntity : GeneratedIntermediateEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
