// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

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
    /// <param name="declaringType">The <see cref="Type"/> that declares the property.</param>
    /// <param name="propertyType">The <see cref="Type"/> of the property.</param>
    /// <param name="name">The property name.</param>
    /// <param name="displayNameInfo">An optional <see cref="DisplayNameInfo"/> that resolves the
    /// display name for the property at validation time. When <see langword="null"/>, the
    /// validation pipeline uses <paramref name="name"/> as the display name.</param>
    protected ValidatablePropertyInfo(
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type declaringType,
        Type propertyType,
        string name,
        DisplayNameInfo? displayNameInfo = null)
    {
        DeclaringType = declaringType;
        PropertyType = propertyType;
        Name = name;
        DisplayNameInfo = displayNameInfo;
    }

    /// <summary>
    /// Gets the type that declares the property.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    internal Type DeclaringType { get; }

    /// <summary>
    /// Gets the property type.
    /// </summary>
    internal Type PropertyType { get; }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Gets the strategy that resolves the display name for the property at validation time,
    /// or <see langword="null"/> when no display name information was supplied.
    /// </summary>
    internal DisplayNameInfo? DisplayNameInfo { get; }

    /// <summary>
    /// Gets the validation attributes for this property.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this property.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <inheritdoc />
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        var property = DeclaringType.GetProperty(Name) ?? throw new InvalidOperationException($"Property '{Name}' not found on type '{DeclaringType.Name}'.");
        var propertyValue = property.GetValue(value);
        var validationAttributes = GetValidationAttributes();

        context.ValidationInitiator ??= this;

        // Calculate and save the current path
        var originalPrefix = context.CurrentValidationPath;
        if (string.IsNullOrEmpty(originalPrefix))
        {
            context.CurrentValidationPath = Name;
        }
        else
        {
            context.CurrentValidationPath = $"{originalPrefix}.{Name}";
        }

        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, DeclaringType) ?? Name;

        context.ValidationContext.DisplayName = displayName;
        context.ValidationContext.MemberName = Name;

        // Check required attribute first
        if (_requiredAttribute is not null || validationAttributes.TryGetRequiredAttribute(out _requiredAttribute))
        {
            var result = _requiredAttribute.GetValidationResult(propertyValue, context.ValidationContext);

            if (result is not null && result != ValidationResult.Success)
            {
                var errorMessage = context.ResolveAttributeErrorMessage(
                    memberName: Name,
                    displayName,
                    declaringType: DeclaringType,
                    attribute: _requiredAttribute,
                    result);

                if (errorMessage is not null)
                {
                    var errorContext = new ValidationErrorContext()
                    {
                        Name = Name,
                        Path = context.CurrentValidationPath,
                        Errors = [errorMessage],
                        Container = value,
                    };
                    context.AddValidationError(errorContext);
                }

                context.CurrentValidationPath = originalPrefix; // Restore prefix
                return;
            }
        }

        // Validate any other attributes
        context.ValidationTasks.Add(ValidationHelpers.ValidateAttributesAsync(validationAttributes, propertyValue, context, (Name, displayName, DeclaringType, value),
            static (context, result, attribute, state) =>
            {
                var (name, displayName, declaringType, container) = state;
                var errorMessage = context.ResolveAttributeErrorMessage(
                    memberName: name,
                    displayName,
                    declaringType: declaringType,
                    attribute,
                    result);

                if (errorMessage is not null)
                {
                    var errorPrefix = context.CurrentValidationPath;
                    var key = errorPrefix.TrimStart('.');
                    var errorContext = new ValidationErrorContext()
                    {
                        Name = name,
                        Path = key,
                        Errors = [errorMessage],
                        Container = container,
                    };
                    context.AddValidationError(errorContext);
                }
            }, cancellationToken));

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

            if (context.ValidationInitiator == this)
            {
                await Task.WhenAll(context.ValidationTasks);
            }
        }
    }
}
