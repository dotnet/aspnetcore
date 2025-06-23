// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a type.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatableTypeInfo : IValidatableInfo
{
    private readonly int _membersCount;
    private readonly List<Type> _subTypes;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatableTypeInfo"/>.
    /// </summary>
    /// <param name="type">The type being validated.</param>
    /// <param name="members">The members that can be validated.</param>
    protected ValidatableTypeInfo(
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

    /// <inheritdoc />
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            return;
        }

        // Check if we've exceeded the maximum depth
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{Type.Name}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }

        var originalPrefix = context.CurrentValidationPath;

        try
        {
            var actualType = value.GetType();

            // First validate members
            for (var i = 0; i < _membersCount; i++)
            {
                await Members[i].ValidateAsync(value, context, cancellationToken);
                context.CurrentValidationPath = originalPrefix;
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
                        context.CurrentValidationPath = originalPrefix;
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
                    if (validationResult != ValidationResult.Success && validationResult.ErrorMessage is not null)
                    {
                        // Create a validation error for each member name that is provided
                        foreach (var memberName in validationResult.MemberNames)
                        {
                            var key = string.IsNullOrEmpty(originalPrefix) ?
                                memberName :
                                $"{originalPrefix}.{memberName}";
                            context.AddOrExtendValidationError(memberName, key, validationResult.ErrorMessage, value);
                        }

                        if (!validationResult.MemberNames.Any())
                        {
                            // If no member names are specified, then treat this as a top-level error
                            context.AddOrExtendValidationError(string.Empty, string.Empty, validationResult.ErrorMessage, value);
                        }
                    }
                }

                // Restore the original validation context properties
                context.ValidationContext.DisplayName = originalDisplayName;
                context.ValidationContext.MemberName = originalMemberName;
            }
        }
        finally
        {
            context.CurrentValidationPath = originalPrefix;
        }
    }
}
