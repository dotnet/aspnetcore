// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Contains validation information for a type.
/// </summary>
public class ValidatableTypeInfo
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
        IReadOnlyList<ValidatableMemberInfo> members,
        bool implementsIValidatableObject,
        IReadOnlyList<Type>? validatableSubTypes = null)
    {
        Type = type;
        Members = members;
        ImplementsIValidatableObject = implementsIValidatableObject;
        ValidatableSubTypes = validatableSubTypes;
    }

    /// <summary>
    /// The type being validated.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The members that can be validated.
    /// </summary>
    public IReadOnlyList<ValidatableMemberInfo> Members { get; }

    /// <summary>
    /// The sub-types that can be validated.
    /// </summary>
    public IReadOnlyList<Type>? ValidatableSubTypes { get; }

    /// <summary>
    /// Indicates whether the type implements IValidatableObject.
    /// </summary>
    public bool ImplementsIValidatableObject { get; }

    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="prefix">The prefix to use for validation errors.</param>
    /// <param name="validationErrors">The dictionary to add validation errors to.</param>
    /// <param name="validatableTypeInfoResolver">The resolver to use for validatable types.</param>
    /// <param name="serviceProvider">The service provider to use for validation context.</param>
    [RequiresUnreferencedCode("Validation requires unreferenced code")]
    public virtual void Validate(object? value, string prefix, Dictionary<string, string[]> validationErrors, IValidatableInfoResolver validatableTypeInfoResolver, IServiceProvider serviceProvider)
    {
        if (value == null)
        {
            return;
        }

        var actualType = value.GetType();

        // Then validate members
        foreach (var member in Members)
        {
            member.Validate(value, prefix, validationErrors, validatableTypeInfoResolver, serviceProvider);
        }

        // Finally validate sub-types if any
        if (ValidatableSubTypes != null)
        {
            foreach (var subType in ValidatableSubTypes)
            {
                if (subType.IsAssignableFrom(actualType))
                {
                    var subTypeInfo = validatableTypeInfoResolver.GetValidatableTypeInfo(subType);
                    subTypeInfo?.Validate(value, prefix, validationErrors, validatableTypeInfoResolver, serviceProvider);
                }
            }
        }

        // Validate IValidatableObject first if implemented
        var trimmedPrefix = prefix.TrimStart('.');
        var hasErrorsForPrefix = validationErrors.Keys.Any(k => k.StartsWith(trimmedPrefix, StringComparison.Ordinal));
        if (!hasErrorsForPrefix && ImplementsIValidatableObject && value is IValidatableObject validatable)
        {
            var validationContext = new ValidationContext(value, serviceProvider: serviceProvider, items: null);
            var validationResults = validatable.Validate(validationContext);

            foreach (var validationResult in validationResults)
            {
                if (validationResult != ValidationResult.Success)
                {
                    var key = string.IsNullOrEmpty(prefix) ?
                        validationResult.MemberNames.First() :
                        $"{prefix.TrimStart('.')}.{validationResult.MemberNames.First()}";

                    if (validationErrors.TryGetValue(key, out var existing) && !existing.Contains(validationResult.ErrorMessage))
                    {
                        validationErrors[key] = existing.Concat(new[] { validationResult.ErrorMessage! }).ToArray();
                    }
                    else
                    {
                        validationErrors[key] = new[] { validationResult.ErrorMessage! };
                    }
                }
            }
        }
    }
}
