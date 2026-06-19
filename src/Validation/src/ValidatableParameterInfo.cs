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
public abstract class ValidatableParameterInfo : IValidatableParameterInfo
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

    /// <inheritdoc />
    /// <remarks>
    /// If the parameter is a collection, each item in the collection will be validated.
    /// If the parameter is not a collection but has a validatable type, the single value will be validated.
    /// </remarks>
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, type: null) ?? Name;

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

        await ValidationHelpers.ValidateAttributesAsync(
            validationAttributes,
            value,
            context,
            (displayName, Name),
            onValidationError,
            cancellationToken);

        // If the parameter is a collection, validate each item
        if (ParameterType.IsEnumerable() && value is IEnumerable enumerable)
        {
            var index = 0;
            var currentPrefix = context.CurrentValidationPath;

            // Note: cloning preserves the same validation options instance.
            var validationOptions = context.ValidationOptions;

            List<Task>? tasks = null;
            var nextUseNeedsClone = false;
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    if (validationOptions.TryGetValidatableTypeInfo(item.GetType(), out var validatableType))
                    {
                        var possiblyClonedContext = nextUseNeedsClone ? context.Clone() : context;
                        nextUseNeedsClone = false;
                        possiblyClonedContext.CurrentValidationPath = string.IsNullOrEmpty(currentPrefix)
                            ? $"{Name}[{index}]"
                            : $"{currentPrefix}.{Name}[{index}]";
                        var enumItemTask = validatableType.ValidateAsync(item, possiblyClonedContext, cancellationToken);
                        if (enumItemTask.IsCompleted)
                        {
                            await enumItemTask;
                        }
                        else
                        {
                            nextUseNeedsClone = true;
                            (tasks ??= new()).Add(enumItemTask);
                        }
                    }
                }
                index++;
            }

            if (tasks is not null)
            {
                await Task.WhenAll(tasks);
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
}
