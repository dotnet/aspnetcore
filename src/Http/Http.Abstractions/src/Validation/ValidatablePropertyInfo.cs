// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Contains validation information for a member of a type.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatablePropertyInfo : IValidatableInfo
{
    private RequiredAttribute? _requiredAttribute;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatablePropertyInfo"/>.
    /// </summary>
    protected ValidatablePropertyInfo(
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type declaringType,
        Type propertyType,
        string name,
        string displayName)
    {
        DeclaringType = declaringType;
        PropertyType = propertyType;
        Name = name;
        DisplayName = displayName;
    }

    /// <summary>
    /// Gets the member type.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
    internal Type DeclaringType { get; }

    /// <summary>
    /// Gets the member type.
    /// </summary>
    internal Type PropertyType { get; }

    /// <summary>
    /// Gets the member name.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Gets the display name for the member as designated by the <see cref="DisplayAttribute"/>.
    /// </summary>
    internal string DisplayName { get; }

    /// <summary>
    /// Gets the validation attributes for this member.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this member.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <inheritdoc />
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        var property = DeclaringType.GetProperty(Name) ?? throw new InvalidOperationException($"Property '{Name}' not found on type '{DeclaringType.Name}'.");
        var propertyValue = property.GetValue(value);
        var validationAttributes = GetValidationAttributes();

        // Calculate and save the current path
        var memberName = GetJsonPropertyName(Name, property, context.SerializerOptions?.PropertyNamingPolicy);
        var originalPrefix = context.CurrentValidationPath;
        if (string.IsNullOrEmpty(originalPrefix))
        {
            context.CurrentValidationPath = memberName;
        }
        else
        {
            context.CurrentValidationPath = $"{originalPrefix}.{memberName}";
        }

        // Format the display name and member name according to JsonPropertyName attribute first, then naming policy
        // If the property has a [Display] attribute (either on property or record parameter), use DisplayName directly without formatting
        var hasDisplayAttribute = HasDisplayAttribute(property);
        context.ValidationContext.DisplayName = hasDisplayAttribute
            ? DisplayName
            : GetJsonPropertyName(DisplayName, property, context.SerializerOptions?.PropertyNamingPolicy);
        context.ValidationContext.MemberName = memberName;

        // Check required attribute first
        if (_requiredAttribute is not null || validationAttributes.TryGetRequiredAttribute(out _requiredAttribute))
        {
            var result = _requiredAttribute.GetValidationResult(propertyValue, context.ValidationContext);

            if (result is not null && result != ValidationResult.Success && result.ErrorMessage is not null)
            {
                context.AddValidationError(context.CurrentValidationPath, [result.ErrorMessage]);
                context.CurrentValidationPath = originalPrefix; // Restore prefix
                return;
            }
        }

        // Validate any other attributes
        ValidateValue(propertyValue, context.CurrentValidationPath, validationAttributes);

        // Check if we've reached the maximum depth before validating complex properties
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{DeclaringType.Name}.{Name}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }

        // Increment depth counter
        context.CurrentDepth++;

        try
        {
            // Handle enumerable values
            if (PropertyType.IsEnumerable() && propertyValue is System.Collections.IEnumerable enumerable)
            {
                var index = 0;
                var currentPrefix = context.CurrentValidationPath;

                foreach (var item in enumerable)
                {
                    context.CurrentValidationPath = $"{currentPrefix}[{index}]";

                    if (item != null)
                    {
                        var itemType = item.GetType();
                        if (context.ValidationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                        {
                            await validatableType.ValidateAsync(item, context, cancellationToken);
                        }
                    }

                    index++;
                }

                // Restore prefix to the property name before validating the next item
                context.CurrentValidationPath = currentPrefix;
            }
            else if (propertyValue != null)
            {
                // Validate as a complex object
                var valueType = propertyValue.GetType();
                if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
                {
                    await validatableType.ValidateAsync(propertyValue, context, cancellationToken);
                }
            }
        }
        finally
        {
            // Always decrement the depth counter and restore prefix
            context.CurrentDepth--;
            context.CurrentValidationPath = originalPrefix;
        }

        void ValidateValue(object? val, string errorPrefix, ValidationAttribute[] validationAttributes)
        {
            for (var i = 0; i < validationAttributes.Length; i++)
            {
                var attribute = validationAttributes[i];
                try
                {
                    var result = attribute.GetValidationResult(val, context.ValidationContext);
                    if (result is not null && result != ValidationResult.Success && result.ErrorMessage is not null)
                    {
                        context.AddOrExtendValidationErrors(errorPrefix.TrimStart('.'), [result.ErrorMessage]);
                    }
                }
                catch (Exception ex)
                {
                    context.AddOrExtendValidationErrors(errorPrefix.TrimStart('.'), [ex.Message]);
                }
            }
        }
    }

    /// <summary>
    /// Gets the effective member name for JSON serialization, considering JsonPropertyName attribute and naming policy.
    /// </summary>
    /// <param name="targetValue">The target value to get the name for.</param>
    /// <param name="property">The property info to get the name for.</param>
    /// <param name="namingPolicy">The JSON naming policy to apply if no JsonPropertyName attribute is present.</param>
    /// <returns>The effective property name for JSON serialization.</returns>
    private static string GetJsonPropertyName(string targetValue, PropertyInfo property, JsonNamingPolicy? namingPolicy)
    {
        var jsonPropertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;

        if (jsonPropertyName is not null)
        {
            return jsonPropertyName;
        }

        if (namingPolicy is not null)
        {
            return namingPolicy.ConvertName(targetValue);
        }

        return targetValue;
    }

    /// <summary>
    /// Determines whether the property has a DisplayAttribute, either directly on the property
    /// or on the corresponding constructor parameter if the declaring type is a record.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns>True if the property has a DisplayAttribute, false otherwise.</returns>
    private bool HasDisplayAttribute(PropertyInfo property)
    {
        // Check if the property itself has the DisplayAttribute with a valid Name
        if (property.GetCustomAttribute<DisplayAttribute>() is { Name: not null })
        {
            return true;
        }

        // Look for a constructor parameter matching the property name (case-insensitive)
        // to account for the record scenario
        foreach (var constructor in DeclaringType.GetConstructors())
        {
            foreach (var parameter in constructor.GetParameters())
            {
                if (string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (parameter.GetCustomAttribute<DisplayAttribute>() is { Name: not null })
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
