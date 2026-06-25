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
public abstract class ValidatablePropertyInfo : IValidatablePropertyInfo, IValidationErrorReporter
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
                ((IValidationErrorReporter)this).ReportError(context, containingObject, _requiredAttribute, result);

                // Restore the validation path mutated above before returning early so that sibling
                // members validated with the same (shared) context observe the original prefix.
                context.CurrentValidationPath = originalPrefix;
                return;
            }
        }

        // Validate any other attributes
        await context.ValidateAttributesAsync(propertyValue, containingObject, this, cancellationToken);

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

                var tracker = context.TrackAsyncValidations();
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        var itemType = item.GetType();
                        if (validationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                        {
                            var currentContext = tracker.NextContext();

                            currentContext.CurrentValidationPath = $"{currentPrefix}[{index}]";
                            try
                            {
                                tracker.Track(validatableType.ValidateAsync(item, currentContext, cancellationToken));
                            }
                            catch (Exception ex)
                            {
                                tracker.Track(Task.FromException(ex));
                            }
                        }
                    }

                    index++;
                }

                await tracker.CompleteAsync();

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

    /// <inheritdoc />
    public virtual void Validate(object containingObject, ValidateContext context)
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
                ((IValidationErrorReporter)this).ReportError(context, containingObject, _requiredAttribute, result);

                // Restore the validation path mutated above before returning early so that sibling
                // members validated with the same (shared) context observe the original prefix.
                context.CurrentValidationPath = originalPrefix;
                return;
            }
        }

        // Validate any other attributes
        context.ValidateAllAttributesSynchronously(propertyValue, containingObject, this);

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

                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        var itemType = item.GetType();
                        if (validationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                        {
                            context.CurrentValidationPath = $"{currentPrefix}[{index}]";
                            validatableType.Validate(item, context);
                        }
                    }

                    index++;
                }

                context.CurrentValidationPath = currentPrefix;
            }
            else if (propertyValue != null)
            {
                // Validate as a complex object
                var valueType = propertyValue.GetType();
                if (validationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
                {
                    validatableType.Validate(propertyValue, context);
                }
            }
        }
        finally
        {
            context.CurrentDepth--;
            context.CurrentValidationPath = originalPrefix;
        }
    }

    ValidationAttribute[] IValidationErrorReporter.GetValidationAttributes()
    {
        return GetValidationAttributes();
    }

    void IValidationErrorReporter.ReportError(ValidateContext context, object? container, ValidationAttribute attribute, ValidationResult result)
    {
        var errorMessage = context.ResolveAttributeErrorMessage(
            memberName: Name,
            context.ValidationContext.DisplayName,
            declaringType: DeclaringType,
            attribute,
            result);

        if (errorMessage is not null)
        {
            var errorContext = new ValidationErrorContext()
            {
                Name = Name,
                Path = context.CurrentValidationPath,
                Errors = [errorMessage],
                Container = container,
            };
            context.AddValidationError(errorContext);
        }
    }
}
