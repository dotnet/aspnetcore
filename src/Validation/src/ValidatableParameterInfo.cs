// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a parameter.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatableParameterInfo : IValidatableParameterInfo, IValidationErrorReporter
{
    private RequiredAttribute? _requiredAttribute;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatableParameterInfo"/>.
    /// </summary>
    /// <param name="parameterType">The <see cref="Type"/> associated with the parameter.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="displayNameInfo">An optional strategy that resolves the
    /// display name for the parameter at validation time. When <see langword="null"/>, the
    /// validation pipeline uses <paramref name="name"/> as the display name.</param>
    protected ValidatableParameterInfo(
        Type parameterType,
        string name,
        DisplayNameInfo? displayNameInfo = null)
    {
        ParameterType = parameterType;
        Name = name;
        DisplayNameInfo = displayNameInfo;
    }

    /// <summary>
    /// Gets the parameter type.
    /// </summary>
    internal Type ParameterType { get; }

    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Gets the strategy that resolves the display name for the parameter at validation time,
    /// or <see langword="null"/> when no display name information was supplied.
    /// </summary>
    internal DisplayNameInfo? DisplayNameInfo { get; }

    /// <summary>
    /// Gets the validation attributes for this parameter.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this parameter.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    private bool ValidateRequiredAttribute(ValidationAttribute[] validationAttributes, object? value, ValidateContext context, ValidationContext? validationContext, string displayName)
    {
        if (_requiredAttribute is not null || validationAttributes.TryGetRequiredAttribute(out _requiredAttribute))
        {
            var result = validationContext is not null
                ? _requiredAttribute.GetValidationResult(value, validationContext)
                : CreateValidationResult(_requiredAttribute.IsValid(value), _requiredAttribute, displayName);

            if (result is not null && result != ValidationResult.Success)
            {
                ((IValidationErrorReporter)this).ReportError(context, displayName, container: null, _requiredAttribute, result);
                return false;
            }
        }

        return true;
    }

    private static ValidationResult? CreateValidationResult(bool isValid, ValidationAttribute attribute, string displayName)
        => isValid
            ? ValidationResult.Success
            : new ValidationResult(attribute.FormatErrorMessage(displayName), null);

    /// <inheritdoc />
    /// <remarks>
    /// If the parameter is a collection, each item in the collection will be validated.
    /// If the parameter is not a collection but has a validatable type, the single value will be validated.
    /// </remarks>
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        var validationAttributes = GetValidationAttributes();

        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, type: null) ?? Name;
        var validationContext = context.CreateValidationContext(value, displayName, Name);

        if (!ValidateRequiredAttribute(validationAttributes, value, context, validationContext, displayName))
        {
            return;
        }

        if (value is null)
        {
            // TODO: The blocker here to support this is that ValidationContext requires a non-null ObjectInstance.
            // We have multiple options to fix this.
            // 1. Don't create a ValidationContext at all, and use the old IsValid API.
            // 2. Create a ValidationContext with a dummy object instance, not really matching ValidationContext API contract/expectation.
            //
            // For now, we only validate RequiredAttribute if present (which we know for sure doesn't need the ValidationContext)
            // We could as well only validate RequiredAttribute, and decide to ship an analyzer
            // to warn if minimal API parameter is nullable and has validation attributes.
            return;
        }

        // Validate against validation attributes
        // Null suppression here is safe. ValidationContext is always non-null when value is not null.
        await context.ValidateAttributesAsync(value, null, this, validationContext!, displayName, cancellationToken);

        // If the parameter is a collection, validate each item
        if (ParameterType.IsEnumerable() && value is IEnumerable enumerable)
        {
            var index = 0;
            var currentPrefix = context.CurrentValidationPath;

            var validationOptions = context.ValidationOptions;

            var tracker = context.TrackAsyncValidations();

            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    if (validationOptions.TryGetValidatableTypeInfo(item.GetType(), out var validatableType))
                    {
                        var currentContext = tracker.NextContext();

                        currentContext.CurrentValidationPath = string.IsNullOrEmpty(currentPrefix)
                            ? $"{Name}[{index}]"
                            : $"{currentPrefix}.{Name}[{index}]";
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

            try
            {
                await tracker.CompleteAsync();
            }
            finally
            {
                context.CurrentValidationPath = currentPrefix;
            }
        }
        // If not enumerable, validate the single value
        else if (value != null)
        {
            var valueType = value.GetType();
            if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
            {
                await validatableType.ValidateAsync(value, context, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// If the parameter is a collection, each item in the collection will be validated.
    /// If the parameter is not a collection but has a validatable type, the single value will be validated.
    /// </remarks>
    public virtual void Validate(object? value, ValidateContext context)
    {
        var validationAttributes = GetValidationAttributes();

        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, type: null) ?? Name;
        var validationContext = context.CreateValidationContext(value, displayName, Name);

        if (!ValidateRequiredAttribute(validationAttributes, value, context, validationContext, displayName))
        {
            return;
        }

        if (value is null)
        {
            // See comment in ValidateAsync.
            return;
        }

        // Validate against validation attributes
        context.ValidateAllAttributesSynchronously(value, null, this, validationContext!, displayName);

        // If the parameter is a collection, validate each item
        if (ParameterType.IsEnumerable() && value is IEnumerable enumerable)
        {
            var index = 0;
            var currentPrefix = context.CurrentValidationPath;

            var validationOptions = context.ValidationOptions;

            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    if (validationOptions.TryGetValidatableTypeInfo(item.GetType(), out var validatableType))
                    {
                        context.CurrentValidationPath = string.IsNullOrEmpty(currentPrefix)
                            ? $"{Name}[{index}]"
                            : $"{currentPrefix}.{Name}[{index}]";
                        try
                        {
                            validatableType.Validate(item, context);
                        }
                        finally
                        {
                            context.CurrentValidationPath = currentPrefix;
                        }
                    }
                }
                index++;
            }
        }
        // If not enumerable, validate the single value
        else if (value != null)
        {
            var valueType = value.GetType();
            if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
            {
                validatableType.Validate(value, context);
            }
        }
    }

    ValidationAttribute[] IValidationErrorReporter.GetValidationAttributes()
    {
        return GetValidationAttributes();
    }

    void IValidationErrorReporter.ReportError(ValidateContext context, string displayName, object? container, ValidationAttribute attribute, ValidationResult result)
    {
        var errorMessage = context.ResolveAttributeErrorMessage(
            memberName: Name,
            displayName,
            declaringType: null,
            attribute,
            result);

        if (errorMessage is not null)
        {
            var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? Name : $"{context.CurrentValidationPath}.{Name}";
            var errorContext = new ValidationErrorContext()
            {
                Name = Name,
                Path = key,
                Errors = [errorMessage],
                Container = null,
            };
            context.AddValidationError(errorContext);
        }
    }
}
