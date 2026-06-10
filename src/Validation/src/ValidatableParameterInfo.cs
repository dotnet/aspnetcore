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
public abstract class ValidatableParameterInfo : IValidatableInfo
{
    private RequiredAttribute? _requiredAttribute;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatableParameterInfo"/>.
    /// </summary>
    /// <param name="parameterType">The <see cref="Type"/> associated with the parameter.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="displayNameInfo">An optional <see cref="DisplayNameInfo"/> that resolves the
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

    /// <inheritdoc />
    /// <remarks>
    /// If the parameter is a collection, each item in the collection will be validated.
    /// If the parameter is not a collection but has a validatable type, the single value will be validated.
    /// </remarks>
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        // Skip validation if value is null and parameter is optional
        if (value == null && ParameterType.IsNullable())
        {
            return;
        }

        context.ValidationInitiator ??= this;

        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, declaringType: null) ?? Name;

        context.ValidationContext.DisplayName = displayName;
        context.ValidationContext.MemberName = Name;

        var validationAttributes = GetValidationAttributes();

        if (_requiredAttribute is not null || validationAttributes.TryGetRequiredAttribute(out _requiredAttribute))
        {
            var result = _requiredAttribute.GetValidationResult(value, context.ValidationContext);

            if (result is not null && result != ValidationResult.Success)
            {
                var errorMessage = context.ResolveAttributeErrorMessage(
                    memberName: Name,
                    displayName,
                    declaringType: null,
                    attribute: _requiredAttribute,
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

                return;
            }
        }

        // Validate against validation attributes
        Action<ValidateContext, ValidationResult, ValidationAttribute, (string displayName, string Name)> onValidationError = static (context, result, attribute, state) =>
        {
            var (displayName, name) = state;
            var errorMessage = context.ResolveAttributeErrorMessage(
                memberName: name,
                displayName,
                declaringType: null,
                attribute,
                result);

            if (errorMessage is not null)
            {
                var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? name : $"{context.CurrentValidationPath}.{name}";
                var errorContext = new ValidationErrorContext()
                {
                    Name = name,
                    Path = key,
                    Errors = [errorMessage],
                    Container = null,
                };
                context.AddValidationError(errorContext);
            }
        };

        context.ValidationTasks.Add(ValidationHelpers.ValidateAttributesAsync(
            validationAttributes,
            value,
            context,
            (displayName, Name),
            onValidationError,
            cancellationToken));

        // If the parameter is a collection, validate each item
        if (ParameterType.IsEnumerable() && value is IEnumerable enumerable)
        {
            var index = 0;
            var currentPrefix = context.CurrentValidationPath;

            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    var clonedContext = context.Clone();
                    clonedContext.CurrentValidationPath = string.IsNullOrEmpty(currentPrefix)
                        ? $"{Name}[{index}]"
                        : $"{currentPrefix}.{Name}[{index}]";

                    if (clonedContext.ValidationOptions.TryGetValidatableTypeInfo(item.GetType(), out var validatableType))
                    {
                        context.ValidationTasks.Add(validatableType.ValidateAsync(item, clonedContext, cancellationToken));
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
                context.ValidationTasks.Add(validatableType.ValidateAsync(value, context, cancellationToken));
            }
        }

        if (context.ValidationInitiator == this)
        {
            await Task.WhenAll(context.ValidationTasks);
        }
    }
}
