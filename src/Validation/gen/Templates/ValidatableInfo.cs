file abstract class ValidatableInfo
{
    protected ValidatableInfo()
    {
    }

    private protected abstract void ReportError(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        string displayName,
        object? container,
        global::System.ComponentModel.DataAnnotations.ValidationAttribute attribute,
        global::System.ComponentModel.DataAnnotations.ValidationResult result);

    private protected static bool IsEnumerable(global::System.Type type)
    {
        // Check if type itself is an IEnumerable
        if (type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(global::System.Collections.Generic.IEnumerable<>) ||
            type.GetGenericTypeDefinition() == typeof(global::System.Collections.Generic.ICollection<>) ||
            type.GetGenericTypeDefinition() == typeof(global::System.Collections.Generic.List<>) ||
            type.GetGenericTypeDefinition() == typeof(global::System.Collections.Generic.IList<>)))
        {
            return true;
        }

        // Or an array
        if (type.IsArray)
        {
            return true;
        }

        // Then evaluate if it implements IEnumerable and is not a string
        if (typeof(global::System.Collections.IEnumerable).IsAssignableFrom(type) &&
            type != typeof(string))
        {
            return true;
        }

        return false;
    }

    private protected static bool ImplementsInterface(global::System.Type type, global::System.Type interfaceType)
    {
        global::System.ArgumentNullException.ThrowIfNull(type);
        global::System.ArgumentNullException.ThrowIfNull(interfaceType);

        if (!interfaceType.IsInterface)
        {
            throw new global::System.ArgumentException($"Type {interfaceType.FullName} is not an interface.", nameof(interfaceType));
        }

        return interfaceType.IsAssignableFrom(type);
    }

    private protected static bool TryGetRequiredAttribute(global::System.ComponentModel.DataAnnotations.ValidationAttribute[] attributes, out global::System.ComponentModel.DataAnnotations.RequiredAttribute? requiredAttribute)
    {
        foreach (var attribute in attributes)
        {
            if (attribute is global::System.ComponentModel.DataAnnotations.RequiredAttribute requiredAttr)
            {
                requiredAttribute = requiredAttr;
                return true;
            }
        }

        requiredAttribute = null;
        return false;
    }

    private protected static string? ResolveAttributeErrorMessage(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        string memberName,
        string displayName,
        global::System.Type declaringType,
        global::System.ComponentModel.DataAnnotations.ValidationAttribute attribute,
        global::System.ComponentModel.DataAnnotations.ValidationResult result)
    {
        if (attribute.ErrorMessageResourceType is not null)
        {
            return result.ErrorMessage;
        }

        if (context.ServiceProvider?.GetService(typeof(global::Microsoft.Extensions.Localization.IStringLocalizerFactory)) is not global::Microsoft.Extensions.Localization.IStringLocalizerFactory localizerFactory)
        {
            return result.ErrorMessage;
        }

        var lookupKey = !string.IsNullOrEmpty(attribute.ErrorMessage)
            ? attribute.ErrorMessage
            : context.ValidationOptions.MessageKeyProvider?.Invoke(new global::Microsoft.Extensions.Validation.ValidationMessageKeyContext
            {
                ValidatorType = attribute.GetType(),
                MemberName = memberName,
                DeclaringType = declaringType,
            });

        if (string.IsNullOrEmpty(lookupKey))
        {
            return result.ErrorMessage;
        }

        var localizer = LocalizationHelpers.CreateStringLocalizer(context, declaringType, localizerFactory);

        var localizedTemplate = localizer[lookupKey!];
        if (localizedTemplate.ResourceNotFound)
        {
            return result.ErrorMessage;
        }

        return FormatErrorMessage(attribute, global::System.Globalization.CultureInfo.CurrentCulture, localizedTemplate.Value, displayName);
    }

    // Keep in sync with DataAnnotationsLocalizer.FormatMessage in
    // src/Components/Endpoints/src/Forms/DataAnnotationsLocalizer.cs, which mirrors this switch for the
    // Blazor SSR client-validation payload.
    private static string FormatErrorMessage(
        global::System.ComponentModel.DataAnnotations.ValidationAttribute attribute,
        global::System.Globalization.CultureInfo culture,
        string messageTemplate,
        string displayName)
        => attribute switch
        {
            global::Microsoft.Extensions.Validation.IValidationMessageFormatter selfFormatter => selfFormatter.FormatMessage(culture, messageTemplate, displayName),
            global::System.ComponentModel.DataAnnotations.CompareAttribute a => string.Format(culture, messageTemplate, displayName, a.OtherPropertyDisplayName ?? a.OtherProperty),
            global::System.ComponentModel.DataAnnotations.FileExtensionsAttribute a => string.Format(culture, messageTemplate, displayName, a.Extensions),
            global::System.ComponentModel.DataAnnotations.LengthAttribute a => string.Format(culture, messageTemplate, displayName, a.MinimumLength, a.MaximumLength),
            global::System.ComponentModel.DataAnnotations.MaxLengthAttribute a => string.Format(culture, messageTemplate, displayName, a.Length),
            global::System.ComponentModel.DataAnnotations.MinLengthAttribute a => string.Format(culture, messageTemplate, displayName, a.Length),
            global::System.ComponentModel.DataAnnotations.RangeAttribute a => string.Format(culture, messageTemplate, displayName, a.Minimum, a.Maximum),
            global::System.ComponentModel.DataAnnotations.RegularExpressionAttribute a => string.Format(culture, messageTemplate, displayName, a.Pattern),
            global::System.ComponentModel.DataAnnotations.StringLengthAttribute a => string.Format(culture, messageTemplate, displayName, a.MaximumLength, a.MinimumLength),
            _ => string.Format(culture, messageTemplate, displayName),
        };

    private protected async global::System.Threading.Tasks.Task ValidateAttributesAsync(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        global::System.ComponentModel.DataAnnotations.ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        global::System.ComponentModel.DataAnnotations.ValidationContext validationContext,
        string displayName,
        global::System.Threading.CancellationToken cancellationToken)
    {
        if (ValidateSynchronousOnly(context, validationAttributes, value, container, validationContext, displayName))
        {
            // Only validate async attributes if synchronous validation passed.
            await ValidateAsynchronousOnlyAsync(context, validationAttributes, value, container, validationContext, displayName, cancellationToken);
        }
    }

    private protected void ValidateAllAttributesSynchronously(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        global::System.ComponentModel.DataAnnotations.ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        global::System.ComponentModel.DataAnnotations.ValidationContext validationContext,
        string displayName)
    {
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];

            var result = attribute.GetValidationResult(value, validationContext);
            if (result is not null && result != global::System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                ReportError(context, displayName, container, attribute, result);
            }
        }
    }

    private bool ValidateSynchronousOnly(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        global::System.ComponentModel.DataAnnotations.ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        global::System.ComponentModel.DataAnnotations.ValidationContext validationContext,
        string displayName)
    {
        bool hasErrors = false;
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];

            if (attribute is global::System.ComponentModel.DataAnnotations.AsyncValidationAttribute)
            {
                continue;
            }

            var result = attribute.GetValidationResult(value, validationContext);
            if (result is not null && result != global::System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                hasErrors = true;
                ReportError(context, displayName, container, attribute, result);
            }
        }

        return !hasErrors;
    }

    private async global::System.Threading.Tasks.Task ValidateAsynchronousOnlyAsync(
        global::Microsoft.Extensions.Validation.ValidateContext context,
        global::System.ComponentModel.DataAnnotations.ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        global::System.ComponentModel.DataAnnotations.ValidationContext validationContext,
        string displayName,
        global::System.Threading.CancellationToken cancellationToken)
    {
        global::System.Threading.CancellationTokenSource? linkedCts = null;
        try
        {
            var tracker = new AsyncValidationTracker(context);
            for (var i = 0; i < validationAttributes.Length; i++)
            {
                var attribute = validationAttributes[i];
                if (attribute is not global::System.ComponentModel.DataAnnotations.AsyncValidationAttribute asyncValidationAttribute)
                {
                    continue;
                }

                linkedCts ??= global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                tracker.Track(
                    GetValidationResultTaskCoreAsync(asyncValidationAttribute, value, container, tracker.NextContext(), validationContext, displayName, cancellationToken, linkedCts));
            }

            await tracker.CompleteAsync();
        }
        finally
        {
            linkedCts?.Dispose();
        }
    }

    private async global::System.Threading.Tasks.Task GetValidationResultTaskCoreAsync(
        global::System.ComponentModel.DataAnnotations.AsyncValidationAttribute attribute,
        object? value,
        object? container,
        global::Microsoft.Extensions.Validation.ValidateContext context,
        global::System.ComponentModel.DataAnnotations.ValidationContext validationContext,
        string displayName,
        global::System.Threading.CancellationToken originalCancellationToken,
        global::System.Threading.CancellationTokenSource linkedCancellationTokenSource)
    {
        // originalCancellationToken is the cancellation token passed to ValidateAttributesAsync.
        // linkedCancellationToken is a LinkedCancellationToken that combines:
        // 1. the original cancellation token, and
        // 2. cancellation when we want to short-circuit on first error.
        try
        {
            var result = await attribute.GetValidationResultAsync(value, validationContext, linkedCancellationTokenSource.Token);
            if (result is not null && result != global::System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                ReportError(context, displayName, container, attribute, result);
                linkedCancellationTokenSource.Cancel();
            }
        }
        catch (global::System.OperationCanceledException) when (linkedCancellationTokenSource.IsCancellationRequested && !originalCancellationToken.IsCancellationRequested)
        {
            // If the original token wasn't cancelled, but ours is cancelled, it means we cancelled to short-circuit.
            // In this case, we want to just ignore this cancellation.
        }
    }

    private protected struct AsyncValidationTracker
    {
        private readonly global::Microsoft.Extensions.Validation.ValidateContext _originalContext;
        private readonly int _originalDepth;
        private readonly string _originalPath;

        private bool _nextNeedsClone;
        private global::Microsoft.Extensions.Validation.ValidateContext _currentContext;
        private global::System.Collections.Generic.List<global::Microsoft.Extensions.Validation.ValidateContext>? _clonedContexts;
        private global::System.Collections.Generic.List<global::System.Threading.Tasks.Task>? _pendingTasks;

        public AsyncValidationTracker(global::Microsoft.Extensions.Validation.ValidateContext context)
        {
            _originalContext = context;
            _currentContext = context;
            _originalDepth = context.CurrentDepth;
            _originalPath = context.CurrentValidationPath;
        }

        // Reuses the context while validations complete synchronously; clones only after one goes async,
        // so two concurrently-running validations never share a context.
        public global::Microsoft.Extensions.Validation.ValidateContext NextContext()
        {
            if (_nextNeedsClone)
            {
                _currentContext = new global::Microsoft.Extensions.Validation.ValidateContext
                {
                    ValidationOptions = _originalContext.ValidationOptions,
                    ServiceProvider = _originalContext.ServiceProvider,
                    CurrentDepth = _originalDepth,
                    CurrentValidationPath = _originalPath,
                };
                (_clonedContexts ??= []).Add(_currentContext);
                _nextNeedsClone = false;
            }

            return _currentContext;
        }

        public void Track(global::System.Threading.Tasks.Task validationTask)
        {
            if (validationTask.IsCompletedSuccessfully)
            {
                return; // synchronous: keep using the same context
            }

            _nextNeedsClone = true; // the next item must get its own clone
            (_pendingTasks ??= []).Add(validationTask);
        }

        // Stays fully synchronous when nothing was tracked; otherwise awaits all and merges clone errors back.
        public readonly global::System.Threading.Tasks.Task<bool> CompleteAsync()
            => _pendingTasks is null ? global::System.Threading.Tasks.Task.FromResult(false) : AwaitAndMergeAsync(_pendingTasks, _clonedContexts, _originalContext);

        private static async global::System.Threading.Tasks.Task<bool> AwaitAndMergeAsync(global::System.Collections.Generic.List<global::System.Threading.Tasks.Task> pendingTasks, global::System.Collections.Generic.List<global::Microsoft.Extensions.Validation.ValidateContext>? clonedContexts, global::Microsoft.Extensions.Validation.ValidateContext originalContext)
        {
            await global::System.Threading.Tasks.Task.WhenAll(pendingTasks);
            return MergeErrorsFromClonedContexts(clonedContexts, originalContext);
        }

        private static bool MergeErrorsFromClonedContexts(global::System.Collections.Generic.List<global::Microsoft.Extensions.Validation.ValidateContext>? clonedContexts, global::Microsoft.Extensions.Validation.ValidateContext originalContext)
        {
            if (clonedContexts is null)
            {
                return false;
            }

            bool hasErrors = false;
            foreach (var clonedContext in clonedContexts)
            {
                if (clonedContext.ValidationErrors is null)
                {
                    continue;
                }

                foreach (var validationError in clonedContext.ValidationErrors)
                {
                    hasErrors = true;

                    foreach (var errorContext in validationError.Value)
                    {
                        originalContext.AddValidationError(errorContext);
                    }
                }
            }

            return hasErrors;
        }
    }
}
