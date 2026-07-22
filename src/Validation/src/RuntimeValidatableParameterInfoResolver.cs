// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// A runtime-based <see cref="IValidatableInfoResolver"/> that resolves parameter validation
/// information using reflection. This resolver acts as a fallback when no source-generated
/// resolver has been configured for the parameter.
/// </summary>
internal sealed class RuntimeValidatableParameterInfoResolver : IValidatableInfoResolver
{
    /// <inheritdoc />
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableTypeInfo)
    {
        validatableTypeInfo = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableParameterInfo)
    {
        if (parameterInfo.Name is null)
        {
            throw new InvalidOperationException($"Encountered a parameter of type '{parameterInfo.ParameterType}' without a name. Parameters must have a name.");
        }

        // Skip method parameter if it or its type are annotated with SkipValidationAttribute.
        if (parameterInfo.GetCustomAttribute<SkipValidationAttribute>() is not null ||
            parameterInfo.ParameterType.GetCustomAttribute<SkipValidationAttribute>() is not null)
        {
            validatableParameterInfo = null;
            return false;
        }

        var validationAttributes = parameterInfo.GetCustomAttributes<ValidationAttribute>().ToArray();

        // If there are no validation attributes and this type is not a complex type
        // we don't need to validate it. Complex types without attributes are still
        // validatable because we want to run the validations on the properties.
        if (validationAttributes.Length == 0 && !IsComplexType(parameterInfo.ParameterType))
        {
            validatableParameterInfo = null;
            return false;
        }

        var displayNameInfo = ResolveDisplayInfo(parameterInfo);

        validatableParameterInfo = new RuntimeValidatableParameterInfo(
            parameterType: parameterInfo.ParameterType,
            name: parameterInfo.Name,
            displayNameInfo: displayNameInfo,
            validationAttributes: validationAttributes
        );
        return true;
    }

    private static DisplayNameInfoBase? ResolveDisplayInfo(ParameterInfo parameterInfo)
    {
        var displayAttribute = parameterInfo.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute is { ResourceType: not null, Name: not null })
        {
            return new ParameterReflectionDisplayName(displayAttribute);
        }

        if (displayAttribute?.Name is not null)
        {
            return new LiteralDisplayName(displayAttribute.Name);
        }

        var displayNameAttribute = parameterInfo.GetCustomAttribute<DisplayNameAttribute>();
        if (displayNameAttribute is not null)
        {
            return new LiteralDisplayName(displayNameAttribute.DisplayName);
        }

        return null;
    }

    private static bool IsComplexType(Type type)
    {
        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeOnly) ||
            type == typeof(DateOnly) ||
            type == typeof(TimeSpan) ||
            type == typeof(Guid) ||
            type == typeof(System.Security.Claims.ClaimsPrincipal) ||
            type == typeof(CancellationToken) ||
            type == typeof(System.IO.Stream) ||
            type == typeof(System.IO.Pipelines.PipeReader))
        {
            return false;
        }

        if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            return IsComplexType(nullableType);
        }

        return type.IsClass || type.IsValueType;
    }

    internal static bool IsEnumerable(Type type)
    {
        if (type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
             type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
             type.GetGenericTypeDefinition() == typeof(List<>) ||
             type.GetGenericTypeDefinition() == typeof(IList<>)))
        {
            return true;
        }

        if (type.IsArray)
        {
            return true;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            return true;
        }

        return false;
    }

    private abstract class DisplayNameInfoBase
    {
        public abstract string? GetDisplayName(ValidateContext context, string memberName, Type? type);
    }

    private sealed class LiteralDisplayName(string literal) : DisplayNameInfoBase
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? type)
        {
            var localizer = context.ValidationOptions.Localizer;
            if (localizer is null)
            {
                return literal;
            }

            return localizer.ResolveDisplayName(new DisplayNameLocalizationContext
            {
                Type = type,
                DisplayName = literal,
                MemberName = memberName,
            }) ?? literal;
        }
    }

    private sealed class ParameterReflectionDisplayName(DisplayAttribute attribute) : DisplayNameInfoBase
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? type)
            => attribute.GetName();
    }

    private sealed class RuntimeValidatableParameterInfo : IValidatableParameterInfo
    {
        private static readonly object _throwawayObjectInstance = new();

        private readonly Type _parameterType;
        private readonly string _name;
        private readonly DisplayNameInfoBase? _displayNameInfo;
        private readonly ValidationAttribute[] _validationAttributes;
        private RequiredAttribute? _requiredAttribute;

        public RuntimeValidatableParameterInfo(
            Type parameterType,
            string name,
            DisplayNameInfoBase? displayNameInfo,
            ValidationAttribute[] validationAttributes)
        {
            _parameterType = parameterType;
            _name = name;
            _displayNameInfo = displayNameInfo;
            _validationAttributes = validationAttributes;
        }

        public async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
        {
            var displayName = _displayNameInfo?.GetDisplayName(context, _name, type: null) ?? _name;
            var validationContext = new ValidationContext(_throwawayObjectInstance, displayName, context.ServiceProvider, null)
            {
                MemberName = _name
            };

            if (!ValidateRequiredAttribute(value, context, validationContext, displayName))
            {
                return;
            }

            await ValidateAttributesAsync(context, _validationAttributes, value, validationContext, displayName, cancellationToken);

            if (IsEnumerable(_parameterType) && value is IEnumerable enumerable)
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
                                ? $"{_name}[{index}]"
                                : $"{currentPrefix}.{_name}[{index}]";
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
            else if (value != null)
            {
                var valueType = value.GetType();
                if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
                {
                    await validatableType.ValidateAsync(value, context, cancellationToken);
                }
            }
        }

        public void Validate(object? value, ValidateContext context)
        {
            var displayName = _displayNameInfo?.GetDisplayName(context, _name, type: null) ?? _name;
            var validationContext = new ValidationContext(_throwawayObjectInstance, displayName, context.ServiceProvider, null)
            {
                MemberName = _name
            };

            if (!ValidateRequiredAttribute(value, context, validationContext, displayName))
            {
                return;
            }

            ValidateAllAttributesSynchronously(context, _validationAttributes, value, validationContext, displayName);

            if (IsEnumerable(_parameterType) && value is IEnumerable enumerable)
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
                                ? $"{_name}[{index}]"
                                : $"{currentPrefix}.{_name}[{index}]";
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
            else if (value != null)
            {
                var valueType = value.GetType();
                if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
                {
                    validatableType.Validate(value, context);
                }
            }
        }

        private bool ValidateRequiredAttribute(object? value, ValidateContext context, ValidationContext? validationContext, string displayName)
        {
            if (_requiredAttribute is not null || TryGetRequiredAttribute(_validationAttributes, out _requiredAttribute))
            {
                var result = validationContext is not null
                    ? _requiredAttribute.GetValidationResult(value, validationContext)
                    : CreateValidationResult(_requiredAttribute.IsValid(value), _requiredAttribute, displayName);

                if (result is not null && result != ValidationResult.Success)
                {
                    ReportError(context, displayName, _requiredAttribute, result);
                    return false;
                }
            }

            return true;
        }

        private void ReportError(ValidateContext context, string displayName, ValidationAttribute attribute, ValidationResult result)
        {
            var errorMessage = ResolveAttributeErrorMessage(context, _name, displayName, declaringType: null, attribute, result);
            if (errorMessage is not null)
            {
                var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? _name : $"{context.CurrentValidationPath}.{_name}";
                context.AddValidationError(new ValidationError
                {
                    Name = _name,
                    Path = key,
                    Errors = [errorMessage],
                    Container = null,
                });
            }
        }

        private static ValidationResult? CreateValidationResult(bool isValid, ValidationAttribute attribute, string displayName)
            => isValid
                ? ValidationResult.Success
                : new ValidationResult(attribute.FormatErrorMessage(displayName), null);

        private static bool TryGetRequiredAttribute(ValidationAttribute[] attributes, [NotNullWhen(true)] out RequiredAttribute? requiredAttribute)
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

        private static string? ResolveAttributeErrorMessage(ValidateContext context, string memberName, string displayName, Type? declaringType, ValidationAttribute attribute, ValidationResult result)
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

        private async Task ValidateAttributesAsync(
            ValidateContext context,
            ValidationAttribute[] attributes,
            object? value,
            ValidationContext validationContext,
            string displayName,
            CancellationToken cancellationToken)
        {
            if (ValidateSynchronousOnly(context, attributes, value, validationContext, displayName))
            {
                await ValidateAsynchronousOnlyAsync(context, attributes, value, validationContext, displayName, cancellationToken);
            }
        }

        private void ValidateAllAttributesSynchronously(
            ValidateContext context,
            ValidationAttribute[] attributes,
            object? value,
            ValidationContext validationContext,
            string displayName)
        {
            foreach (var attribute in attributes)
            {
                var result = attribute.GetValidationResult(value, validationContext);
                if (result is not null && result != ValidationResult.Success)
                {
                    ReportError(context, displayName, attribute, result);
                }
            }
        }

        private bool ValidateSynchronousOnly(
            ValidateContext context,
            ValidationAttribute[] attributes,
            object? value,
            ValidationContext validationContext,
            string displayName)
        {
            var hasErrors = false;
            foreach (var attribute in attributes)
            {
                if (attribute is AsyncValidationAttribute)
                {
                    continue;
                }

                var result = attribute.GetValidationResult(value, validationContext);
                if (result is not null && result != ValidationResult.Success)
                {
                    hasErrors = true;
                    ReportError(context, displayName, attribute, result);
                }
            }

            return !hasErrors;
        }

        private async Task ValidateAsynchronousOnlyAsync(
            ValidateContext context,
            ValidationAttribute[] attributes,
            object? value,
            ValidationContext validationContext,
            string displayName,
            CancellationToken cancellationToken)
        {
            CancellationTokenSource? linkedCts = null;
            try
            {
                var tracker = new AsyncValidationTracker(context);
                foreach (var attribute in attributes)
                {
                    if (attribute is not AsyncValidationAttribute asyncValidationAttribute)
                    {
                        continue;
                    }

                    linkedCts ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    tracker.Track(
                        GetValidationResultTaskCoreAsync(asyncValidationAttribute, value, tracker.NextContext(), validationContext, displayName, cancellationToken, linkedCts));
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
            ValidateContext context,
            ValidationContext validationContext,
            string displayName,
            CancellationToken originalCancellationToken,
            CancellationTokenSource linkedCancellationTokenSource)
        {
            try
            {
                var result = await attribute.GetValidationResultAsync(value, validationContext, linkedCancellationTokenSource.Token);
                if (result is not null && result != ValidationResult.Success)
                {
                    ReportError(context, displayName, attribute, result);
                    linkedCancellationTokenSource.Cancel();
                }
            }
            catch (OperationCanceledException) when (linkedCancellationTokenSource.IsCancellationRequested && !originalCancellationToken.IsCancellationRequested)
            {
                // If the original token wasn't cancelled, but ours is cancelled, it means we cancelled to short-circuit.
            }
        }
    }

    private struct AsyncValidationTracker
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
                return;
            }

            _nextNeedsClone = true;
            (_pendingTasks ??= []).Add(validationTask);
        }

        public readonly Task CompleteAsync()
            => _pendingTasks is null ? Task.CompletedTask : AwaitAndMergeAsync(_pendingTasks, _clonedContexts, _originalContext);

        private static async Task AwaitAndMergeAsync(List<Task> pendingTasks, List<ValidateContext>? clonedContexts, ValidateContext originalContext)
        {
            await Task.WhenAll(pendingTasks);
            MergeErrorsFromClonedContexts(clonedContexts, originalContext);
        }

        private static void MergeErrorsFromClonedContexts(List<ValidateContext>? clonedContexts, ValidateContext originalContext)
        {
            if (clonedContexts is null)
            {
                return;
            }

            foreach (var clonedContext in clonedContexts)
            {
                if (clonedContext.ValidationErrors is null)
                {
                    continue;
                }

                foreach (var validationError in clonedContext.ValidationErrors)
                {
                    foreach (var error in validationError.Value)
                    {
                        originalContext.AddValidationError(error);
                    }
                }
            }
        }
    }
}
