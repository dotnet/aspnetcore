// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a type.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="ValidatableTypeInfo"/>.
/// </remarks>
/// <param name="type">The type being validated.</param>
/// <param name="displayName">The display name for the type as designated by <see cref="DisplayAttribute.Name"/>.</param>
/// <param name="displayNameAccessor">A function that resolves the display name using <see cref="DisplayAttribute.ResourceType"/> and <see cref="DisplayAttribute.Name"/>.</param>
/// <param name="members">The members that can be validated.</param>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatableTypeInfo(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
    string? displayName,
    Func<string>? displayNameAccessor,
    IReadOnlyList<ValidatablePropertyInfo> members) : IValidatableInfo
{
    private readonly int _membersCount = members.Count;
    private readonly List<Type> _superTypes = type.GetAllImplementedTypes();

    /// <summary>
    /// Gets the validation attributes for this member.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this member.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <summary>
    /// The type being validated.
    /// </summary>
    internal Type Type { get; } = type;

    /// <summary>
    /// Gets the display name for the member as designated by the <see cref="DisplayAttribute.Name"/>.
    /// </summary>
    internal string? DisplayName { get; } = displayName;

    /// <summary>
    /// Gets the display name for the member as designated by the <see cref="DisplayAttribute.ResourceType"/>
    /// and <see cref="DisplayAttribute.Name"/>.
    /// </summary>
    internal Func<string>? DisplayNameAccessor { get; } = displayNameAccessor;

    /// <summary>
    /// The members that can be validated.
    /// </summary>
    internal IReadOnlyList<ValidatablePropertyInfo> Members { get; } = members;

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

            var displayName = LocalizationHelper.ResolveDisplayName(
                memberName: Type.Name,
                DisplayName,
                DisplayNameAccessor,
                declaringType: Type,
                context.DisplayNameProvider,
                context.ValidationContext);

            context.ValidationContext.DisplayName = displayName;
            context.ValidationContext.MemberName = null;

            // Validate type-level attributes
            ValidateTypeAttributes(value, context, displayName);

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

    private void ValidateTypeAttributes(object? value, ValidateContext context, string displayName)
    {
        var validationAttributes = GetValidationAttributes();
        var errorPrefix = context.CurrentValidationPath;
        var errorMessageProvider = context.ErrorMessageProvider;

        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];
            var result = attribute.GetValidationResult(value, context.ValidationContext);
            if (result is not null && result != ValidationResult.Success)
            {
                var customMessage = LocalizationHelper.TryResolveErrorMessage(
                    attribute,
                    declaringType: Type,
                    displayName,
                    memberName: Type.Name,
                    errorMessageProvider,
                    context.ValidationContext);

                var errorMessage = customMessage ?? result.ErrorMessage;

                if (errorMessage is not null)
                {
                    // Create a validation error for each member name that is provided
                    foreach (var memberName in result.MemberNames)
                    {
                        var key = string.IsNullOrEmpty(errorPrefix) ? memberName : $"{errorPrefix}.{memberName}";
                        context.AddOrExtendValidationError(memberName, key, errorMessage, value);
                    }

                    if (!result.MemberNames.Any())
                    {
                        // If no member names are specified, then treat this as a top-level error
                        context.AddOrExtendValidationError(string.Empty, errorPrefix, errorMessage, value);
                    }
                }
            }
        }
    }

    private void ValidateValidatableObjectInterface(object? value, ValidateContext context)
    {
        if (Type.ImplementsInterface(typeof(IValidatableObject)) && value is IValidatableObject validatable)
        {
            var errorPrefix = context.CurrentValidationPath;
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
                        context.AddOrExtendValidationError(string.Empty, errorPrefix, validationResult.ErrorMessage, value);
                    }
                }
            }
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
