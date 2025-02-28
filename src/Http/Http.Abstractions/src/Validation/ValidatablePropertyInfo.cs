// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
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
    /// <param name="prefix">The prefix to use for validation errors.</param>
    /// <param name="validationErrors">The dictionary to add validation errors to.</param>
    /// <param name="validatableTypeInfoResolver">The resolver to use for validatable types.</param>
    /// <param name="serviceProvider"></param>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Task Validate(object obj, string prefix, Dictionary<string, string[]> validationErrors, IValidatableInfoResolver validatableTypeInfoResolver, IServiceProvider serviceProvider)
    {
        var property = DeclaringType.GetProperty(Name)!;
        var value = property.GetValue(obj);

        var validationAttributes = GetValidationAttributes();

        if (IsRequired && validationAttributes.OfType<RequiredAttribute>().SingleOrDefault() is { } requiredAttribute)
        {
            var result = requiredAttribute.GetValidationResult(value, new ValidationContext(value ?? new object(), serviceProvider, null)
            {
                DisplayName = DisplayName,
                MemberName = Name
            });

            if (result != ValidationResult.Success)
            {
                var key = string.IsNullOrEmpty(prefix) ? Name : $"{prefix.TrimStart('.')}.{Name}";
                validationErrors[key] = [result!.ErrorMessage!];
                return Task.CompletedTask;
            }
        }

        // If this is an enumerable type, validate each item
        if (IsEnumerable && value is System.Collections.IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                var itemPrefix = $"{prefix}.{Name}[{index}]";
                ValidateValue(item, itemPrefix, validationAttributes);
                if (HasValidatableType && item != null)
                {
                    var itemType = item.GetType();
                    var validatableType = validatableTypeInfoResolver.GetValidatableTypeInfo(itemType);
                    validatableType?.Validate(item, itemPrefix, validationErrors, validatableTypeInfoResolver, serviceProvider);
                }
                index++;
            }
        }
        else
        {
            ValidateValue(value, $"{prefix}.{Name}", validationAttributes);
            if (HasValidatableType && value != null)
            {
                var valueType = value.GetType();
                var validatableType = validatableTypeInfoResolver.GetValidatableTypeInfo(valueType);
                validatableType?.Validate(value, $"{prefix}.{Name}", validationErrors, validatableTypeInfoResolver, serviceProvider);
            }
        }

        return Task.CompletedTask;

        void ValidateValue(object? val, string errorPrefix, ValidationAttribute[] validationAttributes)
        {
            foreach (var attribute in validationAttributes)
            {
                try
                {
                    var validationContext = new ValidationContext(obj, serviceProvider, null)
                    {
                        DisplayName = DisplayName,
                        MemberName = Name
                    };

                    var result = attribute.GetValidationResult(val, validationContext);
                    if (result != ValidationResult.Success)
                    {
                        AddValidationError(errorPrefix, [result!.ErrorMessage!]);
                    }
                }
                catch (Exception ex)
                {
                    AddValidationError(errorPrefix, [ex.Message]);
                }
            }
        }

        void AddValidationError(string errorPrefix, string[] messages)
        {
            var key = errorPrefix.TrimStart('.');
            if (validationErrors.TryGetValue(key, out var existing))
            {
                validationErrors[key] = [.. existing, .. messages];
            }
            else
            {
                validationErrors[key] = messages;
            }
        }
    }
}
