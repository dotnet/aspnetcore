// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Contains validation information for a type.
/// </summary>
public abstract class ValidatableTypeInfo : IValidatableInfo
{
    private readonly int _membersCount;
    private readonly IEnumerable<Type> _subTypes;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatableTypeInfo"/>.
    /// </summary>
    /// <param name="type">The type being validated.</param>
    /// <param name="members">The members that can be validated.</param>
    public ValidatableTypeInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        IReadOnlyList<ValidatablePropertyInfo> members)
    {
        Type = type;
        Members = members;
        _membersCount = members.Count;
        _subTypes = type.GetAllImplementedTypes();
    }

    /// <summary>
    /// The type being validated.
    /// </summary>
    internal Type Type { get; }

    /// <summary>
    /// The members that can be validated.
    /// </summary>
    internal IReadOnlyList<ValidatablePropertyInfo> Members { get; }

    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken"></param>
    public virtual async ValueTask ValidateAsync(object? value, ValidatableContext context, CancellationToken cancellationToken)
    {
        Debug.Assert(context.ValidationContext is not null);
        if (value == null)
        {
            return;
        }

        // Check if we've exceeded the maximum depth
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.Prefix}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }

        try
        {
            var actualType = value.GetType();
            var originalPrefix = context.Prefix;

            // First validate members
            for (var i = 0; i < _membersCount; i++)
            {
                await Members[i].ValidateAsync(value, context, cancellationToken);
                context.Prefix = originalPrefix;
            }

            // Then validate sub-types if any
            foreach (var subType in _subTypes)
            {
                // Check if the actual type is assignable to the sub-type
                // and validate it if it is
                if (subType.IsAssignableFrom(actualType))
                {
                    if (context.ValidationOptions.TryGetValidatableTypeInfo(subType, out var subTypeInfo))
                    {
                        await subTypeInfo.ValidateAsync(value, context, cancellationToken);
                        context.Prefix = originalPrefix;
                    }
                }
            }

            // Finally validate IValidatableObject if implemented
            if (Type.ImplementsInterface(typeof(IValidatableObject)) && value is IValidatableObject validatable)
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

                        context.AddOrExtendValidationError(key, validationResult.ErrorMessage!);
                    }
                }

                // Restore the original validation context properties
                context.ValidationContext.DisplayName = originalDisplayName;
                context.ValidationContext.MemberName = originalMemberName;
            }

            // Always restore original prefix
            context.Prefix = originalPrefix;
        }
        finally
        {
            // Decrement depth when validation completes
            context.CurrentDepth--;
        }
    }
}
