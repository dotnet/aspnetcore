file abstract class ValidatableParameterInfo : ValidatableInfo, global::Microsoft.Extensions.Validation.IValidatableParameterInfo
{
    private global::System.ComponentModel.DataAnnotations.RequiredAttribute? _requiredAttribute;

    private static readonly object _throwawayObjectInstance = new();

    protected ValidatableParameterInfo(
        global::System.Type parameterType,
        string name,
        DisplayNameInfo? displayNameInfo = null)
    {
        ParameterType = parameterType;
        Name = name;
        DisplayNameInfo = displayNameInfo;
    }

    internal global::System.Type ParameterType { get; }

    internal string Name { get; }

    internal DisplayNameInfo? DisplayNameInfo { get; }

    protected abstract global::System.ComponentModel.DataAnnotations.ValidationAttribute[] GetValidationAttributes();

    private bool ValidateRequiredAttribute(global::System.ComponentModel.DataAnnotations.ValidationAttribute[] validationAttributes, object? value, global::Microsoft.Extensions.Validation.ValidateContext context, global::System.ComponentModel.DataAnnotations.ValidationContext? validationContext, string displayName)
    {
        if (_requiredAttribute is not null || TryGetRequiredAttribute(validationAttributes, out _requiredAttribute))
        {
            var result = validationContext is not null
                ? _requiredAttribute!.GetValidationResult(value, validationContext)
                : CreateValidationResult(_requiredAttribute!.IsValid(value), _requiredAttribute, displayName);

            if (result is not null && result != global::System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                ReportError(context, displayName, container: null, _requiredAttribute, result);
                return false;
            }
        }

        return true;
    }

    private static global::System.ComponentModel.DataAnnotations.ValidationResult? CreateValidationResult(bool isValid, global::System.ComponentModel.DataAnnotations.ValidationAttribute attribute, string displayName)
        => isValid
            ? global::System.ComponentModel.DataAnnotations.ValidationResult.Success
            : new global::System.ComponentModel.DataAnnotations.ValidationResult(attribute.FormatErrorMessage(displayName), null);

    public virtual async global::System.Threading.Tasks.Task ValidateAsync(object? value, global::Microsoft.Extensions.Validation.ValidateContext context, global::System.Threading.CancellationToken cancellationToken)
    {
        var validationAttributes = GetValidationAttributes();

        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, type: null) ?? Name;
        var validationContext = new global::System.ComponentModel.DataAnnotations.ValidationContext(_throwawayObjectInstance, displayName, context.ServiceProvider, null)
        {
            MemberName = Name
        };

        if (!ValidateRequiredAttribute(validationAttributes, value, context, validationContext, displayName))
        {
            return;
        }

        // Validate against validation attributes
        await ValidateAttributesAsync(context, validationAttributes, value, null, validationContext, displayName, cancellationToken);

        // If the parameter is a collection, validate each item
        if (IsEnumerable(ParameterType) && value is global::System.Collections.IEnumerable enumerable)
        {
            var index = 0;
            var currentPrefix = context.CurrentValidationPath;

            var validationOptions = context.ValidationOptions;

            var tracker = new AsyncValidationTracker(context);

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
                        catch (global::System.Exception ex)
                        {
                            tracker.Track(global::System.Threading.Tasks.Task.FromException(ex));
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

    public virtual void Validate(object? value, global::Microsoft.Extensions.Validation.ValidateContext context)
    {
        var validationAttributes = GetValidationAttributes();

        var displayName = DisplayNameInfo?.GetDisplayName(context, Name, type: null) ?? Name;
        var validationContext = new global::System.ComponentModel.DataAnnotations.ValidationContext(_throwawayObjectInstance, displayName, context.ServiceProvider, null)
        {
            MemberName = Name
        };

        if (!ValidateRequiredAttribute(validationAttributes, value, context, validationContext, displayName))
        {
            return;
        }

        // Validate against validation attributes
        ValidateAllAttributesSynchronously(context, validationAttributes, value, null, validationContext!, displayName);

        // If the parameter is a collection, validate each item
        if (IsEnumerable(ParameterType) && value is global::System.Collections.IEnumerable enumerable)
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

    private protected override void ReportError(global::Microsoft.Extensions.Validation.ValidateContext context, string displayName, object? container, global::System.ComponentModel.DataAnnotations.ValidationAttribute attribute, global::System.ComponentModel.DataAnnotations.ValidationResult result)
    {
        var errorMessage = ResolveAttributeErrorMessage(
            context,
            memberName: Name,
            displayName,
            declaringType: null,
            attribute,
            result);

        if (errorMessage is not null)
        {
            var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? Name : $"{context.CurrentValidationPath}.{Name}";
            var errorContext = new global::Microsoft.Extensions.Validation.ValidationError()
            {
                Name = Name,
                Path = key,
                ErrorMessage = errorMessage,
                Container = null,
            };
            context.AddValidationError(errorContext);
        }
    }
}
