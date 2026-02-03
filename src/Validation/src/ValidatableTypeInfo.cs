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
    private readonly List<Type> _superTypes;

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
        _superTypes = type.GetAllImplementedTypes();
    }

    /// <summary>
    /// Gets the validation attributes for this member.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this member.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

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
        var originalErrorCount = context.ValidationErrors?.Count ?? 0;

        try
        {
            // First validate direct members
            await ValidateMembersAsync(value, context, cancellationToken);

            var actualType = value.GetType();

            // Then validate inherited members
            foreach (var superTypeInfo in GetSuperTypeInfos(actualType, context))
            {
                await superTypeInfo.ValidateMembersAsync(value, context, cancellationToken);
            }

            // If any property-level validation errors were found, return early
            if (context.ValidationErrors is not null && context.ValidationErrors.Count > originalErrorCount)
            {
                return;
            }

            // Validate type-level attributes
            ValidateTypeAttributes(value, context);

            // If any type-level attribute errors were found, return early
            if (context.ValidationErrors is not null && context.ValidationErrors.Count > originalErrorCount)
            {
                return;
            }

            // Finally validate IValidatableObject if implemented
            ValidateValidatableObjectInterface(value, context);
        }
        finally
        {
            context.CurrentValidationPath = originalPrefix;
        }
    }

    private async Task ValidateMembersAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        var originalPrefix = context.CurrentValidationPath;

        for (var i = 0; i < _membersCount; i++)
        {
            try
            {
                await Members[i].ValidateAsync(value, context, cancellationToken);

            }
            finally
            {
                context.CurrentValidationPath = originalPrefix;
            }
        }
    }

    private void ValidateTypeAttributes(object? value, ValidateContext context)
    {
        var validationAttributes = GetValidationAttributes();
        var errorPrefix = context.CurrentValidationPath;

        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];
            var result = attribute.GetValidationResult(value, context.ValidationContext);
            if (result is not null && result != ValidationResult.Success && result.ErrorMessage is not null)
            {
                // Create a validation error for each member name that is provided
                foreach (var memberName in result.MemberNames)
                {
                    var key = string.IsNullOrEmpty(errorPrefix) ? memberName : $"{errorPrefix}.{memberName}";
                    context.AddOrExtendValidationError(memberName, key, result.ErrorMessage, value);
                }

                if (!result.MemberNames.Any())
                {
                    // If no member names are specified, then treat this as a top-level error
                    context.AddOrExtendValidationError(string.Empty, errorPrefix, result.ErrorMessage, value);
                }
            }
        }
    }

    private void ValidateValidatableObjectInterface(object? value, ValidateContext context)
    {
        if (Type.ImplementsInterface(typeof(IValidatableObject)) && value is IValidatableObject validatable)
        {
            // Important: Set the DisplayName to the type name for top-level validations
            // and restore the original validation context properties
            var originalDisplayName = context.ValidationContext.DisplayName;
            var originalMemberName = context.ValidationContext.MemberName;
            var errorPrefix = context.CurrentValidationPath;

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
                        var key = string.IsNullOrEmpty(errorPrefix) ? memberName : $"{errorPrefix}.{memberName}";
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

    private IEnumerable<ValidatableTypeInfo> GetSuperTypeInfos(Type actualType, ValidateContext context)
    {
        foreach (var superType in _superTypes.Where(t => t.IsAssignableFrom(actualType)))
        {
            if (context.ValidationOptions.TryGetValidatableTypeInfo(superType, out var found)
                && found is ValidatableTypeInfo superTypeInfo)
            {
                yield return superTypeInfo;
            }
        }
    }
}
