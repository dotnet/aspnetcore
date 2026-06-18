// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a member of a type.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatablePropertyInfo : IValidatablePropertyInfo
{
    private RequiredAttribute? _requiredAttribute;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatablePropertyInfo"/>.
    /// </summary>
    /// <param name="declaringType">The <see cref="Type"/> that declares the property.</param>
    /// <param name="propertyType">The <see cref="Type"/> of the property.</param>
    /// <param name="name">The property name.</param>
    /// <param name="displayNameInfo">An optional strategy that resolves the
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

    private PropertyInfo Property
        => DeclaringType.GetProperty(Name) ?? throw new InvalidOperationException($"Property '{Name}' not found on type '{DeclaringType.Name}'.");

    /// <summary>
    /// Gets the validation attributes for this property.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this property.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <inheritdoc />
    public virtual async Task ValidateAsync(object containingObject, ValidateContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(containingObject);

        var propertyValue = Property.GetValue(containingObject);
        var validationAttributes = GetValidationAttributes();

        var hasAsync = HasAsyncAttribute(validationAttributes);
        var potentiallyClonedContext = hasAsync
            ? context.Clone()
            : context;

        // Calculate and save the current path
        var originalPrefix = context.CurrentValidationPath;

        if (string.IsNullOrEmpty(originalPrefix))
        {
            potentiallyClonedContext.CurrentValidationPath = Name;
        }
        else
        {
            potentiallyClonedContext.CurrentValidationPath = $"{originalPrefix}.{Name}";
        }

        var displayName = DisplayNameInfo?.GetDisplayName(potentiallyClonedContext, Name, DeclaringType) ?? Name;

        potentiallyClonedContext.ValidationContext.DisplayName = displayName;
        potentiallyClonedContext.ValidationContext.MemberName = Name;

        // Check required attribute first
        if (_requiredAttribute is not null || validationAttributes.TryGetRequiredAttribute(out _requiredAttribute))
        {
            var result = _requiredAttribute.GetValidationResult(propertyValue, potentiallyClonedContext.ValidationContext);

            if (result is not null && result != ValidationResult.Success)
            {
                var errorMessage = potentiallyClonedContext.ResolveAttributeErrorMessage(
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
                        Path = potentiallyClonedContext.CurrentValidationPath,
                        Errors = [errorMessage],
                        Container = containingObject,
                    };
                    potentiallyClonedContext.AddValidationError(errorContext);
                }

                // Restore the validation path mutated above before returning early so that sibling
                // members validated with the same (shared) context observe the original prefix.
                potentiallyClonedContext.CurrentValidationPath = originalPrefix;
                return;
            }
        }

        // Validate any other attributes
        var attributesValidationTask = ValidationHelpers.ValidateAttributesAsync(validationAttributes, propertyValue, potentiallyClonedContext, (Name, displayName, DeclaringType, containingObject),
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
            }, cancellationToken);

        await attributesValidationTask;

        var validationOptions = potentiallyClonedContext.ValidationOptions;

        // Check if we've reached the maximum depth before validating complex properties
        if (potentiallyClonedContext.CurrentDepth >= validationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {validationOptions.MaxDepth} exceeded at '{potentiallyClonedContext.CurrentValidationPath}' in '{DeclaringType.Name}.{Name}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }

        // Increment depth counter
        potentiallyClonedContext.CurrentDepth++;

        try
        {
            // Handle enumerable values
            if (PropertyType.IsEnumerable() && propertyValue is System.Collections.IEnumerable enumerable)
            {
                var index = 0;
                var currentPrefix = potentiallyClonedContext.CurrentValidationPath;

                List<Task>? tasks = null;

                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        var itemType = item.GetType();
                        if (validationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                        {
                            if (validatableType is ValidatableTypeInfo builtInValidatableInfo &&
                                builtInValidatableInfo.IsGuaranteedToBeSynchronous(item, validationOptions, context.CurrentDepth))
                            {
                                potentiallyClonedContext.CurrentValidationPath = $"{currentPrefix}[{index}]";

                                await validatableType.ValidateAsync(item, potentiallyClonedContext, cancellationToken);
                            }
                            else
                            {
                                var clonedContextForEnumerable = potentiallyClonedContext.Clone();
                                clonedContextForEnumerable.CurrentValidationPath = $"{currentPrefix}[{index}]";

                                (tasks ??= new()).Add(validatableType.ValidateAsync(item, clonedContextForEnumerable, cancellationToken));
                            }
                        }
                    }

                    index++;
                }

                if (tasks is not null)
                {
                    await Task.WhenAll(tasks);
                }

                potentiallyClonedContext.CurrentValidationPath = currentPrefix;
            }
            else if (propertyValue != null)
            {
                // Validate as a complex object
                var valueType = propertyValue.GetType();
                if (validationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
                {
                    if (validatableType is ValidatableTypeInfo builtInValidatableInfo &&
                        builtInValidatableInfo.IsGuaranteedToBeSynchronous(propertyValue, validationOptions, context.CurrentDepth))
                    {
                        await builtInValidatableInfo.ValidateAsync(propertyValue, potentiallyClonedContext, cancellationToken);
                    }
                    else
                    {
                        var clonedForComplexObject = potentiallyClonedContext.Clone();
                        await validatableType.ValidateAsync(propertyValue, clonedForComplexObject, cancellationToken);
                    }
                }
            }
        }
        finally
        {
            if (!hasAsync)
            {
                potentiallyClonedContext.CurrentDepth--;
                potentiallyClonedContext.CurrentValidationPath = originalPrefix;
            }
        }
    }

    private static bool HasAsyncAttribute(ValidationAttribute[] validationAttributes)
    {
        foreach (var attr in validationAttributes)
        {
            if (attr is AsyncValidationAttribute)
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsGuaranteedToBeSynchronous(object containingObject, ValidationOptions options, int currentDepth)
    {
        foreach (var attr in GetValidationAttributes())
        {
            if (attr is AsyncValidationAttribute)
            {
                return false;
            }
        }

        var propertyValue = Property.GetValue(containingObject);
        if (PropertyType.IsEnumerable() && propertyValue is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    var itemType = item.GetType();
                    if (options.TryGetValidatableTypeInfo(itemType, out var validatableType))
                    {
                        if (validatableType is not ValidatableTypeInfo builtInInfo ||
                            !builtInInfo.IsGuaranteedToBeSynchronous(item, options, currentDepth + 1))
                        {
                            return false;
                        }
                    }
                }
            }
        }
        else if (propertyValue != null)
        {
            var valueType = propertyValue.GetType();
            if (options.TryGetValidatableTypeInfo(valueType, out var validatableType))
            {
                if (validatableType is not ValidatableTypeInfo builtInInfo ||
                    !builtInInfo.IsGuaranteedToBeSynchronous(propertyValue, options, currentDepth + 1))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
