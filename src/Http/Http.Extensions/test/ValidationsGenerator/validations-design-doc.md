## Summary

Disclaimer: this document is a joint effort by Safia and Copilot. :smile:

This document outlines the design details of a framework-agnostic implementation of complex object validation built on top of the `System.ComponentModel` validation attributes and APIs.

## Motivation and goals

Historically, whenever a framework wants to implement a data validation feature for its data models, it must implement the logic for discovering validatable types, walking the type graph, invoking the validation provider, gathering validation errors, and handling deeply nested or infinitely recursive type structures.

This exercise has been replicated in multiple implementations including MVC's model validation and Blazor's validation experiments. These implementations may have subtle differences in behavior and have to maintain their own implementations of model validation.

The goal of this proposal is to implement a generic layer for for the discovery of validatable types and the implementation of validation logic that can plug in to any consuming framework (minimal APIs, Blazor, etc.)

## Proposed API

All APIs proposed below are net-new and reside in the `Microsoft.AspNetCore.Http.Abstractions` assembly.

```csharp
namespace Microsoft.AspNetCore.Http.Validation;

// Base types for validation information

/// <summary>
/// Contains validation information for a type.
/// </summary>
public abstract class ValidatableTypeInfo
{
    public Type Type { get; }
    public IReadOnlyList<ValidatablePropertyInfo> Members { get; }
    public bool ImplementsIValidatableObject { get; }
    public Type[]? ValidatableSubTypes { get; }

    protected ValidatableTypeInfo(Type type, IEnumerable<ValidatablePropertyInfo> members,
        bool implementsIValidatableObject, Type[]? validatableSubTypes = null);

    public Task Validate(object? value, ValidatableContext context);
}

/// <summary>
/// Contains validation information for a member of a type.
/// </summary>
public abstract class ValidatablePropertyInfo
{
    public Type DeclaringType { get; }
    public Type PropertyType { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public bool IsNullable { get; }
    public bool IsRequired { get; }
    public bool HasValidatableType { get; }
    public bool IsEnumerable { get; }

    protected ValidatablePropertyInfo(Type containingType, Type propertyType, string name,
        string displayName, bool isEnumerable, bool isNullable,
        bool isRequired, bool hasValidatableType);

    protected abstract ValidationAttribute[] GetValidationAttributes();
    public Task Validate(object obj, ValidatableContext context);
}

/// <summary>
/// Contains validation information for a parameter.
/// </summary>
public abstract class ValidatableParameterInfo
{
    public string Name { get; }
    public string DisplayName { get; }
    public bool IsNullable { get; }
    public bool IsRequired { get; }
    public bool HasValidatableType { get; }
    public bool IsEnumerable { get; }

    protected ValidatableParameterInfo(string name, string displayName, bool isNullable,
        bool isRequired, bool hasValidatableType, bool isEnumerable);

    protected abstract ValidationAttribute[] GetValidationAttributes();
    public Task Validate(object? value, ValidatableContext context);
}

// Validator discovery and registration

/// <summary>
/// Interface for resolvers that provide validation metadata
/// </summary>
public interface IValidatableInfoResolver
{
    ValidatableTypeInfo? GetValidatableTypeInfo(Type type);
    ValidatableParameterInfo? GetValidatableParameterInfo(ParameterInfo parameterInfo);
}

/// <summary>
/// Configuration options for validation
/// </summary>
public class ValidationOptions
{
    public IList<IValidatableInfoResolver> Resolvers { get; } = [];
    public int MaxDepth { get; set; } = 32;

    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out ValidatableTypeInfo? validatableTypeInfo);
    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out ValidatableParameterInfo? validatableParameterInfo);
}

/// <summary>
/// Context for validation operations
/// </summary>
public sealed class ValidatableContext
{
    public ValidationContext? ValidationContext { get; set; }
    public string Prefix { get; set; }
    public required ValidationOptions ValidationOptions { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

/// <summary>
/// Marker attribute to indicate a type should be validated
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ValidatableTypeAttribute : Attribute { }

// Registration extensions
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering validation services
/// </summary>
public static class ValidationServiceCollectionExtensions
{
    /// <summary>
    /// Adds validation services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    public static IServiceCollection AddValidation(this IServiceCollection services);

    /// <summary>
    /// Adds validation services to the specified <see cref="IServiceCollection"/> with the provided options.
    /// </summary>
    public static IServiceCollection AddValidation(this IServiceCollection services,
        Action<ValidationOptions>? configureOptions);
}
```


## Usage Examples

The following demonstrates how the API can be consumed to support model validation in minimal APIs via an endpoint filter implementation.

```csharp
internal static class ValidationEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters();
        var options = context.ApplicationServices.GetService<IOptions<ValidationOptions>>()?.Value;
        if (options is null)
        {
            return next;
        }
        var validatableParameters = parameters
            .Select(p => options.TryGetValidatableParameterInfo(p, out var validatableParameter) ? validatableParameter : null);
        var validatableContext = new ValidatableContext { ValidationOptions = options };
        return async (context) =>
        {
            validatableContext.ValidationErrors?.Clear();

            for (var i = 0; i < context.Arguments.Count; i++)
            {
                var validatableParameter = validatableParameters.ElementAt(i);

                var argument = context.Arguments[i];
                if (argument is null || validatableParameter is null)
                {
                    continue;
                }
                var validationContext = new ValidationContext(argument, context.HttpContext.RequestServices, items: null);
                validatableContext.ValidationContext = validationContext;
                await validatableParameter.Validate(argument, validatableContext);
            }

            if (validatableContext.ValidationErrors is { Count: > 0 })
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.HttpContext.Response.ContentType = "application/problem+json";
                return await ValueTask.FromResult(new HttpValidationProblemDetails(validatableContext.ValidationErrors));
            }

            return await next(context);
        };
    }
}
```

The following demonstrates how the API can be used to enable validation, alongside the validations source generator for a minimal API and highlights the types of validatable arguments that are supported by the generator.

```csharp
// Example of using validation with source generator in a minimal API

// 1. Create a web application and add validation
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddValidation();
var app = builder.Build();

// 2. Define validatable types with the ValidatableType attribute
[ValidatableType]
public class Customer
{
    [Required]
    public string Name { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [Range(18, 120)]
    [Display(Name = "Customer Age")]
    public int Age { get; set; }

    // Complex property with nested validation
    public Address HomeAddress { get; set; } = new Address();
}

public class Address
{
    [Required]
    public string Street { get; set; }

    [Required]
    public string City { get; set; }

    [StringLength(5)]
    public string ZipCode { get; set; }
}

// 3. Define a type implementing IValidatableObject for custom validation
[ValidatableType]
public class Order : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    [Required]
    public string ProductName { get; set; }

    public int Quantity { get; set; }

    // Custom validation logic using IValidatableObject
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity <= 0)
        {
            yield return new ValidationResult(
                "Quantity must be greater than zero",
                [nameof(Quantity)]);
        }
    }
}

// 4. Define endpoints with parameter validation
app.MapGet("/customers/{id}", ([Range(1, int.MaxValue)] int id) =>
    $"Getting customer with ID: {id}");

// 5. Define endpoints with complex object validation
app.MapPost("/customers", (Customer customer) =>
{
    // Validation happens automatically before this code runs
    return TypedResults.Created($"/customers/{customer.Name}", customer);
});

app.MapPost("/orders", (Order order) =>
{
    // Both attribute validation and IValidatableObject.Validate are called automatically
    return TypedResults.Created($"/orders/{order.OrderId}", order);
});

// 6. Use a custom validation attribute
public class EvenNumberAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is int number)
        {
            return number % 2 == 0;
        }
        return false;
    }
}

app.MapPost("/products", ([EvenNumberAttribute(ErrorMessage = "Product ID must be even")] int productId,
    [Required] string name) =>
{
    return TypedResults.Ok(new { productId, name });
});

app.Run();
```

## Implementation Details

### Default Validation Behavior of Validatable Type Info

The `ValidatableTypeInfo.Validate` method follows these steps when validating an object:

1. **Null check**: If the value being validated is null, it immediately returns without validation unless the type is marked as required.

2. **RequiredAttribute handling**: `RequiredAttribute`s are validated before other attributes. If the requiredness check fails, remaining validation attributes are not applied.

3. **Depth limit check**: Before processing nested objects, it checks if the current validation depth exceeds `MaxDepth` (default 32) to prevent stack overflows from circular references or extremely deep object graphs.

4. **Property validation**: Iterates through each property defined in `Members` collection:
   - Gets the property value from the object
   - Applies validation attributes defined on that property
   - For nullable properties, skips validation if the value is null (unless marked required)
   - Handles collections by validating each item in the collection if the property is enumerable

5. **IValidatableObject support**: If the type implements `IValidatableObject`, it calls the `Validate` method after validating individual properties, collecting any additional validation results.

6. **Error aggregation**: Validation errors are added to the `ValidationErrors` dictionary in the context with property names as keys (prefixed if nested) and error messages as values.

7. **Recursive validation**: For properties with complex types that have their own validation requirements, it recursively validates those objects with an updated context prefix to maintain the property path.

### Validation Error Handling

Validation errors are collected in a `Dictionary<string, string[]>` where:
- Keys are property names (including paths for nested properties like `Customer.HomeAddress.Street`)
- Values are arrays of error messages for each property

This format is compatible with ASP.NET Core's `ValidationProblemDetails` for consistent error responses.

### Parameter Validation

The `ValidatableParameterInfo` class provides similar validation for method parameters:

1. Validates attributes applied directly to parameters
2. For complex types, delegates to the appropriate `ValidatableTypeInfo`
3. Supports special handling for common parameter types (primitives, strings, collections)

The validation endpoint filter demonstrates integration with minimal APIs, automatically validating all parameters before the endpoint handler executes.

### Source Generation

The validation system leverages a source generator to:

1. Analyze types marked with `[ValidatableType]` at build time
2. Analyze minimal API endpoints at build-time to automatically discover validatable types without an attribute
3. Generate concrete implementations of `ValidatableTypeInfo` and `ValidatablePropertyInfo`
4. Intercept the `AddValidation` call in user code and add the generated `IValidatableInfoResolver` to the list of resolvers available in the `ValidationOptions`
5. Pre-compiles and caches instances of ValidationAttributes uniquely hashed by their type and initialization arguments

The source generator creates a specialized `IValidatableInfoResolver` implementation that can handle all your validatable types and parameters without runtime reflection overhead.

```csharp
file class GeneratedValidatableInfoResolver : IValidatableInfoResolver
{
    public ValidatableTypeInfo? GetValidatableTypeInfo(Type type)
    {
        // Fast type lookups with no reflection
        if (type == typeof(Customer))
        {
            return CreateCustomerType();
        }
        if (type == typeof(Address))
        {
            return CreateAddressType();
        }
        // Other types...

        return null;
    }

    public ValidatableParameterInfo? GetValidatableParameterInfo(ParameterInfo parameterInfo)
    {
        // Parameter validation info with compile-time knowledge
        if (parameterInfo.Name == "id" && parameterInfo.ParameterType == typeof(int))
        {
            return CreateParameterInfoId();
        }
        // Other parameters...

        return null;
    }

    // Pre-generated factory methods for each type
    private ValidatableTypeInfo CreateCustomerType()
    {
        return new GeneratedValidatableTypeInfo(
            type: typeof(Customer),
            members: [
                // Pre-compiled property validation info
                new GeneratedValidatablePropertyInfo(
                    containingType: typeof(Customer),
                    propertyType: typeof(string),
                    name: "Name",
                    displayName: "Name",
                    isEnumerable: false,
                    isNullable: false,
                    isRequired: true,
                    hasValidatableType: false,
                    validationAttributes: [
                        // Pre-created validation attributes
                        ValidationAttributeCache.GetOrCreateValidationAttribute(
                            typeof(RequiredAttribute),
                            Array.Empty<string>(),
                            new Dictionary<string, string>())
                    ]),
                // Other properties...
            ],
            implementsIValidatableObject: false);
    }

    // Other factory methods...
}
```

The generator emits a `ValidationAttributeCache` to support compiling and caching `ValidationAttributes` by their type and arguments.

```csharp
// Generated ValidationAttribute storage and creation
[GeneratedCode("Microsoft.AspNetCore.Http.ValidationsGenerator", "42.42.42.42")]
file static class ValidationAttributeCache
{
    private static readonly ConcurrentDictionary<string, ValidationAttribute?> _cache = new();

    public static ValidationAttribute? GetOrCreateValidationAttribute(
        Type attributeType,
        string[] arguments,
        IReadOnlyDictionary<string, string> namedArguments)
    {
        // Creates validation attributes efficiently with arguments and properties
        return _cache.GetOrAdd($"{attributeType.FullName}|{string.Join(",", arguments)}|{string.Join(",", namedArguments.Select(x => $"{x.Key}={x.Value}"))}", _ =>
        {
            var type = attributeType;
            ValidationAttribute? attribute = null;

            // Special handling for common attributes with optimization
            if (arguments.Length == 0)
            {
                attribute = type switch
                {
                    Type t when t == typeof(RequiredAttribute) => new RequiredAttribute(),
                    Type t when t == typeof(EmailAddressAttribute) => new EmailAddressAttribute(),
                    Type t when t == typeof(PhoneAttribute) => new PhoneAttribute(),
                    // Other attribute types...
                    _ => null
                };
            }
            else if (type == typeof(StringLengthAttribute))
            {
                if (!int.TryParse(arguments[0], out var maxLength))
                    throw new ArgumentException($"Invalid maxLength value for StringLengthAttribute: {arguments[0]}");
                attribute = new StringLengthAttribute(maxLength);
            }
            else if (type == typeof(RangeAttribute) && arguments.Length == 2)
            {
                if (int.TryParse(arguments[0], out var min) && int.TryParse(arguments[1], out var max))
                    attribute = new RangeAttribute(min, max);
                else if (double.TryParse(arguments[0], out var dmin) && double.TryParse(arguments[1], out var dmax))
                    attribute = new RangeAttribute(dmin, dmax);
            }
            // Other attribute constructors...

            // Apply named arguments as properties after construction
            foreach (var namedArg in namedArguments)
            {
                var prop = type.GetProperty(namedArg.Key);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(attribute, Convert.ChangeType(namedArg.Value, prop.PropertyType));
                }
            }

            return attribute;
        });
    }
}
```

The generator also creates strongly-typed implementations of the abstract validation classes:

```csharp
file sealed class GeneratedValidatablePropertyInfo : ValidatablePropertyInfo
{
    private readonly ValidationAttribute[] _validationAttributes;

    public GeneratedValidatablePropertyInfo(
        Type containingType,
        Type propertyType,
        string name,
        string displayName,
        bool isEnumerable,
        bool isNullable,
        bool isRequired,
        bool hasValidatableType,
        ValidationAttribute[] validationAttributes)
        : base(containingType, propertyType, name, displayName,
              isEnumerable, isNullable, isRequired, hasValidatableType)
    {
        _validationAttributes = validationAttributes;
    }

    protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
}
```

The generator emits an interceptor to the `AddValidation` method that injects the generated `ITypeInfoResolver` into the options object.

```csharp
file static class GeneratedServiceCollectionExtensions
{
    public static IServiceCollection AddValidation(
        this IServiceCollection services,
        Action<ValidationOptions>? configureOptions)
    {
        return ValidationServiceCollectionExtensions.AddValidation(services, options =>
        {
            options.Resolvers.Insert(0, new GeneratedValidatableInfoResolver());
            if (configureOptions is not null)
            {
                configureOptions(options);
            }
        });
    }
}
```

### Validation Extensibility

Similar to existing validation options solutions, users can customize the behavior of the validation system by:

- Custom `ValidationAttribute` implementations
- `IValidatableObject` implementations for complex validation logic

In addition to this, this implementation supports defining vustom validation behavior by defining custom `IValidatableInfoResolver` implementations and inserting them into the `ValidationOptions.Resolvers` property.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidation(options =>
{
    // Add custom resolver before the generated one to give it higher priority
    options.Resolvers.Insert(0, new CustomValidatableInfoResolver());
});


var app = builder.Build();

app.MapPost("/payments", (PaymentInfo payment, [FromQuery] decimal amount) =>
{
    // Both payment and amount will be validated using the custom validators
    return TypedResults.Ok(new { PaymentAccepted = true });
});

app.Run();

public class PaymentInfo
{
    public string CreditCardNumber { get; set; } = string.Empty;
    public string CardholderName { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; }
    public string CVV { get; set; } = string.Empty;
}

public class CustomValidatableInfoResolver : IValidatableInfoResolver
{
    // Provide validation info for specific types
    public ValidatableTypeInfo? GetValidatableTypeInfo(Type type)
    {
        // Example: Special handling for a specific type
        if (type == typeof(PaymentInfo))
        {
            // Create custom validation rules for PaymentInfo type
            return new CustomPaymentInfoTypeInfo();
        }

        return null; // Return null to let other resolvers handle other types
    }

    // Provide validation info for parameters
    public ValidatableParameterInfo? GetValidatableParameterInfo(ParameterInfo parameterInfo)
    {
        // Example: Special validation for payment amount parameters
        if (parameterInfo.Name == "amount" && parameterInfo.ParameterType == typeof(decimal))
        {
            return new CustomAmountParameterInfo();
        }

        return null; // Return null to let other resolvers handle other parameters
    }

    // Example of custom ValidatableTypeInfo implementation
    private class CustomPaymentInfoTypeInfo : ValidatableTypeInfo
    {
        public CustomPaymentInfoTypeInfo()
            : base(typeof(PaymentInfo), CreateValidatableProperties(), implementsIValidatableObject: false)
        {
        }

        private static IEnumerable<ValidatablePropertyInfo> CreateValidatableProperties()
        {
            // Define custom validation logic for properties
            yield return new CustomPropertyInfo(
                typeof(PaymentInfo),
                typeof(string),
                "CreditCardNumber",
                "Credit Card Number",
                isEnumerable: false,
                isNullable: false,
                isRequired: true,
                hasValidatableType: false);

            // Add more properties as needed
        }
    }

    // Example of custom ValidatableParameterInfo implementation
    private class CustomAmountParameterInfo : ValidatableParameterInfo
    {
        private static readonly ValidationAttribute[] _attributes = new ValidationAttribute[]
        {
            new RangeAttribute(0.01, 10000.00) { ErrorMessage = "Amount must be between $0.01 and $10,000.00" }
        };

        public CustomAmountParameterInfo()
            : base("amount", "Payment Amount", isNullable: false, isRequired: true,
                  hasValidatableType: false, isEnumerable: false)
        {
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _attributes;
    }

    // Example of custom property info implementation
    private class CustomPropertyInfo : ValidatablePropertyInfo
    {
        private static readonly ValidationAttribute[] _ccAttributes = new ValidationAttribute[]
        {
            new CreditCardAttribute(),
            new RequiredAttribute(),
            new StringLengthAttribute(19) { MinimumLength = 13, ErrorMessage = "Credit card number must be between 13 and 19 digits" }
        };

        public CustomPropertyInfo(
            Type containingType, Type propertyType, string name, string displayName,
            bool isEnumerable, bool isNullable, bool isRequired, bool hasValidatableType)
            : base(containingType, propertyType, name, displayName,
                  isEnumerable, isNullable, isRequired, hasValidatableType)
        {
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _ccAttributes;
    }
}
```

## Open Questions and Future Considerations

* How should this validation system plugin to other validation systems like Blazor? This implementation has been applied to non-minimal APIs scenarios in practice.
* Does the implementation account for all trimming and native AoT compat scenarios? The existing Options validation generator applies custom implementations of built-in ValidationAttributes to support native AoT compt that haven't been accounted for here.
* Should a more robust validation result type be considered? The implementation currently relies on a lazily-initialized Dictionary for this but we can consider something more robust.


