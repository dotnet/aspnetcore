// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Contains validation information for a type.
/// </summary>
public abstract class ValidatableTypeInfo
{
    /// <summary>
    /// Creates a new instance of <see cref="ValidatableTypeInfo"/>.
    /// </summary>
    /// <param name="type">The type being validated.</param>
    /// <param name="members">The members that can be validated.</param>
    /// <param name="implementsIValidatableObject">Indicates whether the type implements IValidatableObject.</param>
    /// <param name="validatableSubTypes">The sub-types that can be validated.</param>
    public ValidatableTypeInfo(
        Type type,
        IReadOnlyList<ValidatablePropertyInfo> members,
        bool implementsIValidatableObject,
        IReadOnlyList<Type>? validatableSubTypes = null)
    {
        Type = type;
        Members = members;
        IsIValidatableObject = implementsIValidatableObject;
        ValidatableSubTypes = validatableSubTypes;
    }

    /// <summary>
    /// The type being validated.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The members that can be validated.
    /// </summary>
    public IReadOnlyList<ValidatablePropertyInfo> Members { get; }

    /// <summary>
    /// The sub-types that can be validated.
    /// </summary>
    public IReadOnlyList<Type>? ValidatableSubTypes { get; }

    /// <summary>
    /// Indicates whether the type implements IValidatableObject.
    /// </summary>
    public bool IsIValidatableObject { get; }

    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context"></param>
    public Task Validate(object? value, ValidatableContext context)
    {
        if (value == null)
        {
            return Task.CompletedTask;
        }

        var actualType = value.GetType();
        var originalPrefix = context.Prefix;

        // First validate members
        foreach (var member in Members)
        {
            member.Validate(value, context);
            context.Prefix = originalPrefix;
        }

        // Then validate sub-types if any
        if (ValidatableSubTypes != null)
        {
            foreach (var subType in ValidatableSubTypes)
            {
                if (subType.IsAssignableFrom(actualType))
                {
                    if (context.ValidationOptions.TryGetValidatableTypeInfo(subType, out var subTypeInfo))
                    {
                        subTypeInfo.Validate(value, context);
                        context.Prefix = originalPrefix;
                    }
                }
            }
        }

        // Finally validate IValidatableObject if implemented
        if (IsIValidatableObject && value is IValidatableObject validatable)
        {
            // Important: Set the DisplayName to the type name for top-level validations
            // and restore the original validation context properties
            var originalDisplayName = context.ValidationContext.DisplayName;
            var originalMemberName = context.ValidationContext.MemberName;

            // Set the display name to the class name for IValidatableObject validation
            context.ValidationContext.DisplayName = Type.Name;
            context.ValidationContext.MemberName = null;

            var validationResults = validatable.Validate(context.ValidationContext);
            foreach (var validationResult in validationResults)
            {
                if (validationResult != ValidationResult.Success)
                {
                    var memberName = validationResult.MemberNames.First();
                    var key = string.IsNullOrEmpty(originalPrefix) ?
                        memberName :
                        $"{originalPrefix}.{memberName}";

                    if (context.ValidationErrors.TryGetValue(key, out var existing) && !existing.Contains(validationResult.ErrorMessage))
                    {
                        context.ValidationErrors[key] = [.. existing, validationResult.ErrorMessage!];
                    }
                    else
                    {
                        context.ValidationErrors[key] = [validationResult.ErrorMessage!];
                    }
                }
            }

            // Restore the original validation context properties
            context.ValidationContext.DisplayName = originalDisplayName;
            context.ValidationContext.MemberName = originalMemberName;
        }

        // Always restore original prefix
        context.Prefix = originalPrefix;
        return Task.CompletedTask;
    }
}
