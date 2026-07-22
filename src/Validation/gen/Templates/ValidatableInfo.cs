file abstract class ValidatableInfo
{
    protected ValidatableInfo()
    {
    }

    private protected abstract void ReportError(
        ValidateContext context,
        string displayName,
        object? container,
        ValidationAttribute attribute,
        ValidationResult result);

    private protected static bool IsEnumerable(Type type)
    {
        // Check if type itself is an IEnumerable
        if (type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
            type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
            type.GetGenericTypeDefinition() == typeof(List<>) ||
            type.GetGenericTypeDefinition() == typeof(IList<>)))
        {
            return true;
        }

        // Or an array
        if (type.IsArray)
        {
            return true;
        }

        // Then evaluate if it implements IEnumerable and is not a string
        if (typeof(IEnumerable).IsAssignableFrom(type) &&
            type != typeof(string))
        {
            return true;
        }

        return false;
    }

    private protected static bool ImplementsInterface(Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(interfaceType);

        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException($"Type {interfaceType.FullName} is not an interface.", nameof(interfaceType));
        }

        return interfaceType.IsAssignableFrom(type);
    }

    private protected static bool TryGetRequiredAttribute(ValidationAttribute[] attributes, [NotNullWhen(true)] out RequiredAttribute? requiredAttribute)
    {
        foreach (var attribute in attributes)
        {
            if (attribute is RequiredAttribute requiredAttr)
            {
                requiredAttribute = requiredAttr;
                return true;
            }
        }

        requiredAttribute = null;
        return false;
    }

    private protected static string? ResolveAttributeErrorMessage(
        ValidateContext context,
        string memberName,
        string displayName,
        Type? declaringType,
        ValidationAttribute attribute,
        ValidationResult result)
    {
        if (context.ValidationOptions.Localizer is null || attribute.ErrorMessageResourceType is not null)
        {
            return result.ErrorMessage;
        }

        var localizationContext = new ErrorMessageLocalizationContext
        {
            MemberName = memberName,
            DisplayName = displayName,
            DeclaringType = declaringType,
            Attribute = attribute,
        };

        return context.ValidationOptions.Localizer.ResolveErrorMessage(localizationContext) ?? result.ErrorMessage;
    }

    private protected async Task ValidateAttributesAsync(
        ValidateContext context,
        ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        ValidationContext validationContext,
        string displayName,
        CancellationToken cancellationToken)
    {
        // NOTE: In case there are no async validation attributes, there should be no performance impact.
        // The async state machine is a class only in Debug builds. But in Release it's a struct.
        // So it will be efficient.
        // And if this method completed synchronously because no async validation attributes exist, this
        // will returned the same cached instance as Task.CompletedTask.
        if (ValidateSynchronousOnly(context, validationAttributes, value, container, validationContext, displayName))
        {
            // Only validate async attributes if synchronous validation passed.
            await ValidateAsynchronousOnlyAsync(context, validationAttributes, value, container, validationContext, displayName, cancellationToken);
        }
    }

    private protected void ValidateAllAttributesSynchronously(
        ValidateContext context,
        ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        ValidationContext validationContext,
        string displayName)
    {
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];

            var result = attribute.GetValidationResult(value, validationContext);
            if (result is not null && result != ValidationResult.Success)
            {
                ReportError(context, displayName, container, attribute, result);
            }
        }
    }

    private bool ValidateSynchronousOnly(
        ValidateContext context,
        ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        ValidationContext validationContext,
        string displayName)
    {
        bool hasErrors = false;
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];

            if (attribute is AsyncValidationAttribute)
            {
                continue;
            }

            var result = attribute.GetValidationResult(value, validationContext);
            if (result is not null && result != ValidationResult.Success)
            {
                hasErrors = true;
                ReportError(context, displayName, container, attribute, result);
            }
        }

        return !hasErrors;
    }

    private async Task ValidateAsynchronousOnlyAsync(
        ValidateContext context,
        ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        ValidationContext validationContext,
        string displayName,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource? linkedCts = null;
        try
        {
            var tracker = new AsyncValidationTracker(context);
            for (var i = 0; i < validationAttributes.Length; i++)
            {
                var attribute = validationAttributes[i];
                if (attribute is not AsyncValidationAttribute asyncValidationAttribute)
                {
                    continue;
                }

                linkedCts ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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

    private async Task GetValidationResultTaskCoreAsync(
        AsyncValidationAttribute attribute,
        object? value,
        object? container,
        ValidateContext context,
        ValidationContext validationContext,
        string displayName,
        CancellationToken originalCancellationToken,
        CancellationTokenSource linkedCancellationTokenSource)
    {
        // originalCancellationToken is the cancellation token passed to ValidateAttributesAsync.
        // linkedCancellationToken is a LinkedCancellationToken that combines:
        // 1. the original cancellation token, and
        // 2. cancellation when we want to short-circuit on first error.
        try
        {
            var result = await attribute.GetValidationResultAsync(value, validationContext, linkedCancellationTokenSource.Token);
            if (result is not null && result != ValidationResult.Success)
            {
                ReportError(context, displayName, container, attribute, result);
                linkedCancellationTokenSource.Cancel();
            }
        }
        catch (OperationCanceledException) when (linkedCancellationTokenSource.IsCancellationRequested && !originalCancellationToken.IsCancellationRequested)
        {
            // If the original token wasn't cancelled, but ours is cancelled, it means we cancelled to short-circuit.
            // In this case, we want to just ignore this cancellation.
        }
    }

    private protected struct AsyncValidationTracker
    {
        private readonly ValidateContext _originalContext;
        private readonly int _originalDepth;
        private readonly string _originalPath;

        private bool _nextNeedsClone;
        private ValidateContext _currentContext;
        private List<ValidateContext>? _clonedContexts;
        private List<Task>? _pendingTasks;

        public AsyncValidationTracker(ValidateContext context)
        {
            _originalContext = context;
            _currentContext = context;
            _originalDepth = context.CurrentDepth;
            _originalPath = context.CurrentValidationPath;
        }

        // Reuses the context while validations complete synchronously; clones only after one goes async,
        // so two concurrently-running validations never share a context.
        public ValidateContext NextContext()
        {
            if (_nextNeedsClone)
            {
                _currentContext = new ValidateContext
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

        public void Track(Task validationTask)
        {
            if (validationTask.IsCompletedSuccessfully)
            {
                return; // synchronous: keep using the same context
            }

            _nextNeedsClone = true; // the next item must get its own clone
            (_pendingTasks ??= []).Add(validationTask);
        }

        // Stays fully synchronous when nothing was tracked; otherwise awaits all and merges clone errors back.
        public readonly Task<bool> CompleteAsync()
            => _pendingTasks is null ? Task.FromResult(false) : AwaitAndMergeAsync(_pendingTasks, _clonedContexts, _originalContext);

        private static async Task<bool> AwaitAndMergeAsync(List<Task> pendingTasks, List<ValidateContext>? clonedContexts, ValidateContext originalContext)
        {
            await Task.WhenAll(pendingTasks);
            return MergeErrorsFromClonedContexts(clonedContexts, originalContext);
        }

        private static bool MergeErrorsFromClonedContexts(List<ValidateContext>? clonedContexts, ValidateContext originalContext)
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
