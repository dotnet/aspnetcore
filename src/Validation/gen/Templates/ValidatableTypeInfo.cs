file abstract class ValidatableTypeInfo : ValidatableInfo, global::Microsoft.Extensions.Validation.IValidatableTypeInfo
{
    private readonly int _membersCount;
    private readonly global::System.Type[] _implementedInterfaces;

    private static readonly object _throwawayObjectInstance = new();

    protected ValidatableTypeInfo(
        [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)] global::System.Type type,
        global::System.Collections.Generic.IReadOnlyList<global::Microsoft.Extensions.Validation.Generated.ValidatablePropertyInfo> members,
        DisplayNameInfo? displayNameInfo = null)
    {
        Type = type;
        Members = members;
        DisplayNameInfo = displayNameInfo;
        _membersCount = members.Count;
        _implementedInterfaces = type.GetInterfaces();
    }

    protected abstract global::System.ComponentModel.DataAnnotations.ValidationAttribute[] GetValidationAttributes();

    [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
    internal global::System.Type Type { get; }

    internal global::System.Collections.Generic.IReadOnlyList<global::Microsoft.Extensions.Validation.Generated.ValidatablePropertyInfo> Members { get; }

    internal DisplayNameInfo? DisplayNameInfo { get; }

    public bool TryFindProperty(string propertyName, global::Microsoft.Extensions.Validation.ValidationOptions validationOptions, out global::Microsoft.Extensions.Validation.IValidatablePropertyInfo? validatablePropertyInfo)
    {
        if (FindLocalMember(propertyName) is { } localMember)
        {
            validatablePropertyInfo = localMember;
            return true;
        }

        foreach (var @interface in _implementedInterfaces)
        {
            if (validationOptions.TryGetValidatableTypeInfo(@interface, out var interfaceTypeInfo) &&
                interfaceTypeInfo.TryFindProperty(propertyName, validationOptions, out validatablePropertyInfo))
            {
                return true;
            }
        }

        var baseType = Type.BaseType;
        while (baseType is not null)
        {
            if (validationOptions.TryGetValidatableTypeInfo(baseType, out var baseTypeTypeInfo))
            {
                return baseTypeTypeInfo.TryFindProperty(propertyName, validationOptions, out validatablePropertyInfo);
            }

            baseType = baseType.BaseType;
        }

        validatablePropertyInfo = null;
        return false;
    }

    private ValidatablePropertyInfo? FindLocalMember(string memberName)
    {
        for (var i = 0; i < _membersCount; i++)
        {
            if (string.Equals(Members[i].Name, memberName, global::System.StringComparison.Ordinal))
            {
                return Members[i];
            }
        }

        return null;
    }

    private void ValidateDepth(global::Microsoft.Extensions.Validation.ValidateContext context)
    {
        // Check if we've exceeded the maximum depth
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new global::System.InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{Type.Name}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }
    }

    public virtual async global::System.Threading.Tasks.Task ValidateAsync(object? value, global::Microsoft.Extensions.Validation.ValidateContext context, global::System.Threading.CancellationToken cancellationToken)
    {
        if (value is null)
        {
            // If we have null value here, the only thing we can validate is the type-level attributes.
            // There are no "members" to validate, and there is no IValidatableObject to validate.
            var display = DisplayNameInfo?.GetDisplayName(context, Type.Name, Type) ?? Type.Name;
            await ValidateAttributesAsync(
                context,
                GetValidationAttributes(),
                value: value,
                container: value,
                new global::System.ComponentModel.DataAnnotations.ValidationContext(_throwawayObjectInstance, display, context.ServiceProvider, null), display, cancellationToken);
            return;
        }

        ValidateDepth(context);

        var originalErrorCount = context.ValidationErrors?.Count ?? 0;

        // First validate direct members
        var tracker = new AsyncValidationTracker(context);
        tracker = ValidateMembers(value, tracker, cancellationToken);

        var actualType = value.GetType();

        // Then validate inherited members
        foreach (var superTypeInfo in GetSuperTypeInfos(actualType, context.ValidationOptions))
        {
            tracker = superTypeInfo.ValidateMembers(value, tracker, cancellationToken);
        }

        var clonedContextsHasErrors = await tracker.CompleteAsync();

        var currentCount = context.ValidationErrors?.Count ?? 0;

        // If any property-level validation errors were found, return early
        if (currentCount > originalErrorCount || clonedContextsHasErrors)
        {
            return;
        }

        var displayName = DisplayNameInfo?.GetDisplayName(context, Type.Name, Type) ?? Type.Name;

        // Validate type-level attributes
        var validationContext = new global::System.ComponentModel.DataAnnotations.ValidationContext(value ?? _throwawayObjectInstance, displayName, context.ServiceProvider, null);

        await ValidateAttributesAsync(context, GetValidationAttributes(), value, value, validationContext, displayName, cancellationToken);

        // If any type-level attribute errors were found, return early
        currentCount = context.ValidationErrors?.Count ?? 0;
        if (currentCount > originalErrorCount)
        {
            return;
        }

        // Finally validate IValidatableObject if implemented
        await ValidateValidatableObjectInterfaceAsync(value, context, validationContext, cancellationToken);
    }

    public virtual void Validate(object? value, global::Microsoft.Extensions.Validation.ValidateContext context)
    {
        if (value == null)
        {
            // If we have null value here, the only thing we can validate is the type-level attributes.
            // There are no "members" to validate, and there is no IValidatableObject to validate.
            var display = DisplayNameInfo?.GetDisplayName(context, Type.Name, Type) ?? Type.Name;
            ValidateAllAttributesSynchronously(
                context,
                GetValidationAttributes(),
                value: value,
                container: value,
                new global::System.ComponentModel.DataAnnotations.ValidationContext(_throwawayObjectInstance, display, context.ServiceProvider, null), display);

            return;
        }

        ValidateDepth(context);

        var originalErrorCount = context.ValidationErrors?.Count ?? 0;

        ValidateMembersSynchronously(value, context);

        var actualType = value.GetType();

        // Then validate inherited members
        foreach (var superTypeInfo in GetSuperTypeInfos(actualType, context.ValidationOptions))
        {
            superTypeInfo.ValidateMembersSynchronously(value, context);
        }

        var currentCount = context.ValidationErrors?.Count ?? 0;

        // If any property-level validation errors were found, return early
        if (currentCount > originalErrorCount)
        {
            return;
        }

        var displayName = DisplayNameInfo?.GetDisplayName(context, Type.Name, Type) ?? Type.Name;

        // Validate type-level attributes
        var validationContext = new global::System.ComponentModel.DataAnnotations.ValidationContext(value ?? _throwawayObjectInstance, displayName, context.ServiceProvider, null);

        ValidateAllAttributesSynchronously(context, GetValidationAttributes(), value, value, validationContext, displayName);

        // If any type-level attribute errors were found, return early
        currentCount = context.ValidationErrors?.Count ?? 0;
        if (currentCount > originalErrorCount)
        {
            return;
        }

        // Finally validate IValidatableObject if implemented
        ValidateValidatableObjectInterface(value, context, validationContext);
    }

    private AsyncValidationTracker ValidateMembers(
        object value,
        AsyncValidationTracker tracker,
        global::System.Threading.CancellationToken cancellationToken)
    {
        for (var i = 0; i < _membersCount; i++)
        {
            var context = tracker.NextContext();

            try
            {
                tracker.Track(Members[i].ValidateAsync(value, context, cancellationToken));
            }
            catch (global::System.Exception ex)
            {
                tracker.Track(global::System.Threading.Tasks.Task.FromException(ex));
            }
        }

        return tracker;
    }

    private void ValidateMembersSynchronously(object value, global::Microsoft.Extensions.Validation.ValidateContext context)
    {
        for (var i = 0; i < _membersCount; i++)
        {
            Members[i].Validate(value, context);
        }
    }

    private async global::System.Threading.Tasks.Task ValidateValidatableObjectInterfaceAsync(object? value, global::Microsoft.Extensions.Validation.ValidateContext context, global::System.ComponentModel.DataAnnotations.ValidationContext validationContext, global::System.Threading.CancellationToken cancellationToken)
    {
        if (ImplementsInterface(Type, typeof(global::System.ComponentModel.DataAnnotations.IValidatableObject)) && value is global::System.ComponentModel.DataAnnotations.IValidatableObject validatable)
        {
            var errorPrefix = context.CurrentValidationPath;
            if (value is global::System.ComponentModel.DataAnnotations.IAsyncValidatableObject asyncValidatable)
            {
                await foreach (var validationResult in asyncValidatable.ValidateAsync(validationContext, cancellationToken))
                {
                    HandleValidationResultForValidatableObject(validationResult, errorPrefix, value, context);
                }
            }
            else
            {
                foreach (var validationResult in validatable.Validate(validationContext))
                {
                    HandleValidationResultForValidatableObject(validationResult, errorPrefix, value, context);
                }
            }
        }
    }

    private static void HandleValidationResultForValidatableObject(global::System.ComponentModel.DataAnnotations.ValidationResult validationResult, string errorPrefix, object? value, global::Microsoft.Extensions.Validation.ValidateContext context)
    {
        if (validationResult != global::System.ComponentModel.DataAnnotations.ValidationResult.Success && validationResult.ErrorMessage is not null)
        {
            // Create a validation error for each member name that is provided
            // We don't support automatic localization of IValidatableObject messages
            foreach (var memberName in validationResult.MemberNames)
            {
                var key = string.IsNullOrEmpty(errorPrefix) ? memberName : $"{errorPrefix}.{memberName}";
                var errorContext = new global::Microsoft.Extensions.Validation.ValidationError()
                {
                    Name = memberName,
                    Path = key,
                    ErrorMessage = validationResult.ErrorMessage,
                    Container = value,
                };
                context.AddValidationError(errorContext);
            }

            if (!global::System.Linq.Enumerable.Any(validationResult.MemberNames))
            {
                // If no member names are specified, then treat this as a top-level error
                var errorContext = new global::Microsoft.Extensions.Validation.ValidationError()
                {
                    Name = string.Empty,
                    Path = string.Empty,
                    ErrorMessage = validationResult.ErrorMessage,
                    Container = value,
                };
                context.AddValidationError(errorContext);
            }
        }
    }

    private void ValidateValidatableObjectInterface(object? value, global::Microsoft.Extensions.Validation.ValidateContext context, global::System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        if (ImplementsInterface(Type, typeof(global::System.ComponentModel.DataAnnotations.IValidatableObject)) && value is global::System.ComponentModel.DataAnnotations.IValidatableObject validatable)
        {
            var errorPrefix = context.CurrentValidationPath;

            foreach (var validationResult in validatable.Validate(validationContext))
            {
                HandleValidationResultForValidatableObject(validationResult, errorPrefix, value, context);
            }
        }
    }

    private global::System.Collections.Generic.IEnumerable<global::Microsoft.Extensions.Validation.Generated.ValidatableTypeInfo> GetSuperTypeInfos(global::System.Type actualType, global::Microsoft.Extensions.Validation.ValidationOptions options)
    {
        foreach (var @interface in _implementedInterfaces)
        {
            if (TryGetValidatableTypeInfo(@interface, actualType, options) is { } superTypeInfo)
            {
                yield return superTypeInfo;
            }
        }

        var baseType = Type.BaseType;
        while (baseType is not null)
        {
            if (TryGetValidatableTypeInfo(baseType, actualType, options) is { } superTypeInfo)
            {
                yield return superTypeInfo;
            }

            baseType = baseType.BaseType;
        }

        static ValidatableTypeInfo? TryGetValidatableTypeInfo(global::System.Type superType, global::System.Type actualType, global::Microsoft.Extensions.Validation.ValidationOptions options)
        {
            if (superType.IsAssignableFrom(actualType) &&
                options.TryGetValidatableTypeInfo(superType, out var found)
                && found is ValidatableTypeInfo superTypeInfo)
            {
                return superTypeInfo;
            }

            return null;
        }
    }

    private protected override void ReportError(global::Microsoft.Extensions.Validation.ValidateContext context, string displayName, object? container, global::System.ComponentModel.DataAnnotations.ValidationAttribute attribute, global::System.ComponentModel.DataAnnotations.ValidationResult result)
    {
        foreach (var memberName in result.MemberNames)
        {
            // Create a validation error for each member name that is provided
            var errorMessage = ResolveAttributeErrorMessage(
                context,
                memberName,
                displayName,
                declaringType: Type,
                attribute,
                result);

            if (errorMessage is not null)
            {
                var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? memberName : $"{context.CurrentValidationPath}.{memberName}";
                var errorContext = new global::Microsoft.Extensions.Validation.ValidationError()
                {
                    Name = memberName,
                    Path = key,
                    ErrorMessage = errorMessage,
                    Container = container,
                };
                context.AddValidationError(errorContext);
            }
        }

        if (!global::System.Linq.Enumerable.Any(result.MemberNames))
        {
            // If no member names are specified, then treat this as a top-level error
            var errorMessage = ResolveAttributeErrorMessage(
                context,
                memberName: Type.Name,
                displayName,
                declaringType: Type,
                attribute,
                result);

            if (errorMessage is not null)
            {
                var errorContext = new global::Microsoft.Extensions.Validation.ValidationError()
                {
                    Name = string.Empty,
                    Path = context.CurrentValidationPath,
                    ErrorMessage = errorMessage,
                    Container = container,
                };
                context.AddValidationError(errorContext);
            }
        }
    }
}
