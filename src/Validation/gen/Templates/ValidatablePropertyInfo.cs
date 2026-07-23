file abstract class ValidatablePropertyInfo : ValidatableInfo, global::Microsoft.Extensions.Validation.IValidatablePropertyInfo
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

        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, DeclaringType) ?? Name;

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
            if (IsEnumerable(PropertyType) && propertyValue is System.Collections.IEnumerable enumerable)
            {
                var index = 0;
                var currentPrefix = context.CurrentValidationPath;

                var tracker = new AsyncValidationTracker(context);
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
                            catch (global::System.Exception ex)
                            {
                                tracker.Track(global::System.Threading.Tasks.Task.FromException(ex));
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

        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, DeclaringType) ?? Name;

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
            if (IsEnumerable(PropertyType) && propertyValue is System.Collections.IEnumerable enumerable)
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
