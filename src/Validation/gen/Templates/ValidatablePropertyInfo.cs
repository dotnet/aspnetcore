<<<<<<< HEAD:src/Validation/src/ValidatablePropertyInfo.cs
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a member of a type.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatablePropertyInfo : IValidatablePropertyInfo, IValidationErrorReporter
=======
file abstract class ValidatablePropertyInfo : ValidatableInfo, global::Microsoft.Extensions.Validation.IValidatablePropertyInfo
>>>>>>> origin/main:src/Validation/gen/Templates/ValidatablePropertyInfo.cs
{
    private global::System.ComponentModel.DataAnnotations.RequiredAttribute? _requiredAttribute;

    protected ValidatablePropertyInfo(
        [param: global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        global::System.Type declaringType,
        global::System.Type propertyType,
        string name,
        DisplayNameInfo? displayNameInfo = null)
    {
        DeclaringType = declaringType;
        PropertyType = propertyType;
        Name = name;
        DisplayNameInfo = displayNameInfo;
    }

    [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
    internal global::System.Type DeclaringType { get; }

    internal global::System.Type PropertyType { get; }

    internal string Name { get; }

    internal DisplayNameInfo? DisplayNameInfo { get; }

    private global::System.Reflection.PropertyInfo Property
        => DeclaringType.GetProperty(Name, global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.DeclaredOnly) ?? throw new global::System.InvalidOperationException($"Property '{Name}' not found on type '{DeclaringType.Name}'.");

    protected abstract global::System.ComponentModel.DataAnnotations.ValidationAttribute[] GetValidationAttributes();

    private void ValidateDepth(global::Microsoft.Extensions.Validation.ValidateContext context)
    {
        // Check if we've reached the maximum depth before validating complex properties
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new global::System.InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{DeclaringType.Name}.{Name}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }
    }

    private bool ValidateRequiredAttribute(global::System.ComponentModel.DataAnnotations.ValidationAttribute[] validationAttributes, global::Microsoft.Extensions.Validation.ValidateContext context, object? propertyValue, object containingObject, global::System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        if (_requiredAttribute is not null || TryGetRequiredAttribute(validationAttributes, out _requiredAttribute))
        {
            var result = _requiredAttribute!.GetValidationResult(propertyValue, validationContext);

            if (result is not null && result != global::System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                ReportError(context, validationContext.DisplayName, containingObject, _requiredAttribute, result);

                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public virtual async global::System.Threading.Tasks.Task ValidateAsync(object containingObject, global::Microsoft.Extensions.Validation.ValidateContext context, global::System.Threading.CancellationToken cancellationToken)
    {
        global::System.ArgumentNullException.ThrowIfNull(containingObject);

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

        var displayName = DisplayNameInfo?.GetDisplayName(context, DeclaringType) ?? Name;

        var validationContext = new global::System.ComponentModel.DataAnnotations.ValidationContext(containingObject, displayName, context.ServiceProvider, null)
        {
            MemberName = Name,
        };

        // Check required attribute first
        if (!ValidateRequiredAttribute(validationAttributes, context, propertyValue, containingObject, validationContext))
        {
            // Restore the validation path mutated above before returning early so that sibling
            // members validated with the same (shared) context observe the original prefix.
            context.CurrentValidationPath = originalPrefix;
            return;
        }

        // Validate any other attributes
        await ValidateAttributesAsync(context, validationAttributes, propertyValue, containingObject, validationContext, displayName, cancellationToken);

        var validationOptions = context.ValidationOptions;

        ValidateDepth(context);

        // Increment depth counter
        context.CurrentDepth++;

        try
        {
            // Handle enumerable values
<<<<<<< HEAD:src/Validation/src/ValidatablePropertyInfo.cs
            if (PropertyType.IsEnumerable() && propertyValue is IEnumerable enumerable)
=======
            if (IsEnumerable(PropertyType) && propertyValue is System.Collections.IEnumerable enumerable)
>>>>>>> origin/main:src/Validation/gen/Templates/ValidatablePropertyInfo.cs
            {
                var index = 0;
                var currentPrefix = context.CurrentValidationPath;

<<<<<<< HEAD:src/Validation/src/ValidatablePropertyInfo.cs
                var tracker = context.TrackAsyncValidations();
=======
                var tracker = new AsyncValidationTracker(context);
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        var itemType = item.GetType();
                        if (validationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                        {
                            var currentContext = tracker.NextContext();
>>>>>>> origin/main:src/Validation/gen/Templates/ValidatablePropertyInfo.cs

                var enumerator = enumerable.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        var (key, item) = enumerator is IDictionaryEnumerator de ? (de.Key, de.Value) : (index, enumerator.Current);

                        if (item is not null)
                        {
                            var itemType = item.GetType();
                            if (validationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                            {
<<<<<<< HEAD:src/Validation/src/ValidatablePropertyInfo.cs
                                var currentContext = tracker.NextContext();

                                currentContext.CurrentValidationPath = $"{currentPrefix}[{key}]";
                                try
                                {
                                    tracker.Track(validatableType.ValidateAsync(item, currentContext, cancellationToken));
                                }
                                catch (Exception ex)
                                {
                                    tracker.Track(Task.FromException(ex));
                                }
=======
                                tracker.Track(validatableType.ValidateAsync(item, currentContext, cancellationToken));
                            }
                            catch (global::System.Exception ex)
                            {
                                tracker.Track(global::System.Threading.Tasks.Task.FromException(ex));
>>>>>>> origin/main:src/Validation/gen/Templates/ValidatablePropertyInfo.cs
                            }
                        }

                        index++;
                    }
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
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
    public virtual void Validate(object containingObject, global::Microsoft.Extensions.Validation.ValidateContext context)
    {
        global::System.ArgumentNullException.ThrowIfNull(containingObject);

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

        var displayName = DisplayNameInfo?.GetDisplayName(context, DeclaringType) ?? Name;

        var validationContext = new global::System.ComponentModel.DataAnnotations.ValidationContext(containingObject, displayName, context.ServiceProvider, null)
        {
            MemberName = Name,
        };

        // Check required attribute first
        if (!ValidateRequiredAttribute(validationAttributes, context, propertyValue, containingObject, validationContext))
        {
            // Restore the validation path mutated above before returning early so that sibling
            // members validated with the same (shared) context observe the original prefix.
            context.CurrentValidationPath = originalPrefix;
            return;
        }

        // Validate any other attributes
        ValidateAllAttributesSynchronously(context, validationAttributes, propertyValue, containingObject, validationContext, displayName);

        var validationOptions = context.ValidationOptions;

        ValidateDepth(context);

        // Increment depth counter
        context.CurrentDepth++;

        try
        {
            // Handle enumerable values
<<<<<<< HEAD:src/Validation/src/ValidatablePropertyInfo.cs
            if (PropertyType.IsEnumerable() && propertyValue is IEnumerable enumerable)
=======
            if (IsEnumerable(PropertyType) && propertyValue is System.Collections.IEnumerable enumerable)
>>>>>>> origin/main:src/Validation/gen/Templates/ValidatablePropertyInfo.cs
            {
                var index = 0;
                var currentPrefix = context.CurrentValidationPath;

                var enumerator = enumerable.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        var (key, item) = enumerator is IDictionaryEnumerator de ? (de.Key, de.Value) : (index, enumerator.Current);

                        if (item is not null)
                        {
                            var itemType = item.GetType();
                            if (validationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                            {
                                context.CurrentValidationPath = $"{currentPrefix}[{key}]";
                                validatableType.Validate(item, context);
                            }
                        }

                        index++;
                    }
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
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

    private protected override void ReportError(global::Microsoft.Extensions.Validation.ValidateContext context, string displayName, object? container, global::System.ComponentModel.DataAnnotations.ValidationAttribute attribute, global::System.ComponentModel.DataAnnotations.ValidationResult result)
    {
        var errorMessage = ResolveAttributeErrorMessage(
            context,
            memberName: Name,
            displayName,
            declaringType: DeclaringType,
            attribute,
            result);

        if (errorMessage is not null)
        {
            var errorContext = new global::Microsoft.Extensions.Validation.ValidationError()
            {
                Name = Name,
                Path = context.CurrentValidationPath,
                ErrorMessage = errorMessage,
                Container = container,
            };
            context.AddValidationError(errorContext);
        }
    }
}
