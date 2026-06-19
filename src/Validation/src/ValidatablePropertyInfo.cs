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
                        Container = containingObject,
                    };
                    context.AddValidationError(errorContext);
                }

                // Restore the validation path mutated above before returning early so that sibling
                // members validated with the same (shared) context observe the original prefix.
                context.CurrentValidationPath = originalPrefix;
                return;
            }
        }

        // Validate any other attributes
        var attributesValidationTask = ValidationHelpers.ValidateAttributesAsync(validationAttributes, propertyValue, context, (Name, displayName, DeclaringType, containingObject),
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

        var validationOptions = context.ValidationOptions;

        // Check if we've reached the maximum depth before validating complex properties
        if (context.CurrentDepth >= validationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {validationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{DeclaringType.Name}.{Name}'. " +
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

                List<Task>? tasks = null;
                //var nextUseNeedsClone = false;

                var originalState = context.CaptureMutableState();
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        var itemType = item.GetType();
                        if (validationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                        {
                            //var clonedContextForEnumerable = nextUseNeedsClone ? context.CopyWithState(originalState) : context;
                            var clonedContextForEnumerable = context.CopyWithState(originalState);
                            //nextUseNeedsClone = false;
                            clonedContextForEnumerable.CurrentValidationPath = $"{currentPrefix}[{index}]";
                            var task = validatableType.ValidateAsync(item, clonedContextForEnumerable, cancellationToken);
                            if (task.IsCompleted)
                            {
                                await task;
                            }
                            else
                            {
                                // nextUseNeedsClone = true;
                                (tasks ??= new()).Add(task);
                            }
                        }
                    }

                    index++;
                }

                if (tasks is not null)
                {
                    await Task.WhenAll(tasks);
                }

                context.CurrentValidationPath = currentPrefix;
            }
            else if (propertyValue != null)
            {
                // Validate as a complex object
                var valueType = propertyValue.GetType();
                if (validationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
                {
                    await validatableType.ValidateAsync(propertyValue, context, cancellationToken);
                }
            }
        }
        finally
        {
            context.CurrentDepth--;
            context.CurrentValidationPath = originalPrefix;
        }
    }
}
