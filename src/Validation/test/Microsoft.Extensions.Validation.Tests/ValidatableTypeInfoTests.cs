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
            yield return new ValidationResult("Salary must be a positive value.", [nameof(Salary)]);
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
