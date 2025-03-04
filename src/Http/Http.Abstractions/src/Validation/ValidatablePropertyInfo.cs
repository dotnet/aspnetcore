// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Contains validation information for a member of a type.
/// </summary>
public abstract class ValidatablePropertyInfo
{
    /// <summary>
    /// Creates a new instance of <see cref="ValidatablePropertyInfo"/>.
    /// </summary>
    public ValidatablePropertyInfo(
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type declaringType,
        Type propertyType,
        string name,
        string displayName,
        bool isEnumerable,
        bool isNullable,
        bool isRequired,
        bool hasValidatableType)
    {
        DeclaringType = declaringType;
        PropertyType = propertyType;
        Name = name;
        DisplayName = displayName;
        IsEnumerable = isEnumerable;
        IsNullable = isNullable;
        IsRequired = isRequired;
        HasValidatableType = hasValidatableType;
    }

    /// <summary>
    /// Gets the member type.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type DeclaringType { get; }

    /// <summary>
    /// Gets the member type.
    /// </summary>
    public Type PropertyType { get; }

    /// <summary>
    /// Gets the member name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the display name for the member as designated by the <see cref="DisplayAttribute"/>.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets whether the member is enumerable.
    /// </summary>
    public bool IsEnumerable { get; }

    /// <summary>
    /// Gets whether the member is nullable.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets whether the member is annotated with the <see cref="RequiredAttribute"/>.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets whether the member's type is validatable.
    /// </summary>
    public bool HasValidatableType { get; }

    /// <summary>
    /// Gets the validation attributes for this member.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this member.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <summary>
    /// Validates the member's value.
    /// </summary>
    /// <param name="obj">The object containing the member to validate.</param>
    /// <param name="context">The context for the validation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task Validate(object obj, ValidatableContext context)
    {
        Debug.Assert(context.ValidationContext is not null);

        var property = DeclaringType.GetProperty(Name)!;
        var value = property.GetValue(obj);
        var validationAttributes = GetValidationAttributes();

        // Calculate and save the current path
        var originalPrefix = context.Prefix;
        if (string.IsNullOrEmpty(originalPrefix))
        {
            context.Prefix = Name;
        }
        else
        {
            context.Prefix = $"{originalPrefix}.{Name}";
        }

        context.ValidationContext.DisplayName = DisplayName;
        context.ValidationContext.MemberName = Name;

        // Check required attribute first
        if (IsRequired && validationAttributes.OfType<RequiredAttribute>().SingleOrDefault() is { } requiredAttribute)
        {
            var result = requiredAttribute.GetValidationResult(value, context.ValidationContext);

            if (result is not null && result != ValidationResult.Success)
            {
                context.AddValidationError(context.Prefix, [result!.ErrorMessage!]);
                context.Prefix = originalPrefix; // Restore prefix
                return Task.CompletedTask;
            }
        }

        // Validate any other attributes
        ValidateValue(value, context.Prefix, validationAttributes);

        // Check if we've reached the maximum depth before validating complex properties
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.Prefix}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }

        // Increment depth counter
        context.CurrentDepth++;

        try
        {
            // Handle enumerable values
            if (IsEnumerable && value is System.Collections.IEnumerable enumerable)
            {
                var index = 0;
                var currentPrefix = context.Prefix;

                foreach (var item in enumerable)
                {
                    context.Prefix = $"{currentPrefix}[{index}]";

                    if (HasValidatableType && item != null)
                    {
                        var itemType = item.GetType();
                        if (context.ValidationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                        {
                            validatableType.Validate(item, context);
                        }
                    }

                    index++;
                }

                // Restore prefix to the property name before validating the next item
                context.Prefix = currentPrefix;
            }
            else if (HasValidatableType && value != null)
            {
                // Validate as a complex object
                var valueType = value.GetType();
                if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
                {
                    validatableType.Validate(value, context);
                }
            }

            return Task.CompletedTask;
        }
        finally
        {
            // Always decrement the depth counter and restore prefix
            context.CurrentDepth--;
            context.Prefix = originalPrefix;
        }

        void ValidateValue(object? val, string errorPrefix, ValidationAttribute[] validationAttributes)
        {
            foreach (var attribute in validationAttributes)
            {
                try
                {
                    var result = attribute.GetValidationResult(val, context.ValidationContext);
                    if (result is not null && result != ValidationResult.Success)
                    {
                        context.AddOrExtendValidationErrors(errorPrefix.TrimStart('.'), [result.ErrorMessage!]);
                    }
                }
                catch (Exception ex)
                {
                    context.AddOrExtendValidationErrors(errorPrefix.TrimStart('.'), [ex.Message]);
                }
            }
        }
    }
}
