// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a type.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatableTypeInfo : IValidatableTypeInfo, IValidationErrorReporter
{
    private readonly int _membersCount;
    private readonly Type[] _implementedInterfaces;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatableTypeInfo"/>.
    /// </summary>
    /// <param name="type">The type being validated.</param>
    /// <param name="members">The members that can be validated.</param>
    /// <param name="displayNameInfo">An optional strategy that resolves the
    /// display name for the type at validation time. When <see langword="null"/>, the validation
    /// pipeline uses <see cref="System.Reflection.MemberInfo.Name"/> of <paramref name="type"/>
    /// as the display name.</param>
    protected ValidatableTypeInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        IReadOnlyList<ValidatablePropertyInfo> members,
        DisplayNameInfo? displayNameInfo = null)
    {
        Type = type;
        Members = members;
        DisplayNameInfo = displayNameInfo;
        _membersCount = members.Count;
        _implementedInterfaces = type.GetInterfaces();
    }

    /// <summary>
    /// Gets the validation attributes applied to this type.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this type.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <summary>
    /// The type being validated.
    /// </summary>
    internal Type Type { get; }

    /// <summary>
    /// The members that can be validated.
    /// </summary>
    internal IReadOnlyList<ValidatablePropertyInfo> Members { get; }

    /// <summary>
    /// Gets the strategy that resolves the display name for the type at validation time,
    /// or <see langword="null"/> when no display name information was supplied.
    /// </summary>
    internal DisplayNameInfo? DisplayNameInfo { get; }

    /// <summary>
    /// Finds the <see cref="ValidatablePropertyInfo"/> for a member with the specified
    /// <paramref name="propertyName"/>, including members inherited from base types or implemented
    /// interfaces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Members declared directly on <see cref="Type"/> take precedence over members inherited
    /// from super-types, matching the order in which <see cref="ValidateAsync(object?, ValidateContext, CancellationToken)"/>
    /// visits members.
    /// </para>
    /// <para>
    /// Inherited members are resolved by looking up each super-type via
    /// <paramref name="validationOptions"/>'s <see cref="ValidationOptions.Resolvers"/>. Super-types that
    /// are not registered with a resolver are silently skipped.
    /// </para>
    /// </remarks>
    /// <param name="propertyName">The CLR name of the property to find.</param>
    /// <param name="validationOptions">The <see cref="ValidationOptions"/> used to resolve metadata for super-types.</param>
    /// <param name="validatablePropertyInfo">The matching <see cref="ValidatablePropertyInfo"/>, or <see langword="null"/> if no
    /// member with the specified name is declared on <see cref="Type"/> or any of its super-types.</param>
    /// <returns>True if the property was found. Otherwise, false.</returns>
    public bool TryFindProperty(string propertyName, ValidationOptions validationOptions, [NotNullWhen(true)] out IValidatablePropertyInfo? validatablePropertyInfo)
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
            if (string.Equals(Members[i].Name, memberName, StringComparison.Ordinal))
            {
                return Members[i];
            }
        }

        return null;
    }

    private void ValidateDepth(ValidateContext context)
    {
        // Check if we've exceeded the maximum depth
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{Type.Name}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }
    }

    /// <inheritdoc />
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            return;
        }

        ValidateDepth(context);

        var originalErrorCount = context.ValidationErrors?.Count ?? 0;

        // First validate direct members
        var tracker = context.TrackAsyncValidations();
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
        await ValidateTypeAttributesAsync(value, context, displayName, cancellationToken);

        // If any type-level attribute errors were found, return early
        currentCount = context.ValidationErrors?.Count ?? 0;
        if (currentCount > originalErrorCount)
        {
            return;
        }

        // Finally validate IValidatableObject if implemented
        await ValidateValidatableObjectInterfaceAsync(value, context, displayName, cancellationToken);
    }

    /// <inheritdoc />
    public virtual void Validate(object? value, ValidateContext context)
    {
        if (value == null)
        {
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
        ValidateTypeAttributes(value, context, displayName);

        // If any type-level attribute errors were found, return early
        currentCount = context.ValidationErrors?.Count ?? 0;
        if (currentCount > originalErrorCount)
        {
            return;
        }

        // Finally validate IValidatableObject if implemented
        ValidateValidatableObjectInterface(value, context, displayName);
    }

    private ValidateContext.AsyncValidationTracker ValidateMembers(
        object value,
        ValidateContext.AsyncValidationTracker tracker,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _membersCount; i++)
        {
            var context = tracker.NextContext();

            try
            {
                tracker.Track(Members[i].ValidateAsync(value, context, cancellationToken));
            }
            catch (Exception ex)
            {
                tracker.Track(Task.FromException(ex));
            }
        }

        return tracker;
    }

    private void ValidateMembersSynchronously(object value, ValidateContext context)
    {
        for (var i = 0; i < _membersCount; i++)
        {
            Members[i].Validate(value, context);
        }
    }

    private async Task ValidateTypeAttributesAsync(object? value, ValidateContext context, string displayName, CancellationToken cancellationToken)
    {
        var originalDisplayName = context.ValidationContext.DisplayName;
        var originalMemberName = context.ValidationContext.MemberName;

        try
        {
            context.ValidationContext.DisplayName = displayName;
            context.ValidationContext.MemberName = null;

            await context.ValidateAttributesAsync(value, value, this, cancellationToken);
        }
        finally
        {
            context.ValidationContext.DisplayName = originalDisplayName;
            context.ValidationContext.MemberName = originalMemberName;
        }
    }

    private void ValidateTypeAttributes(object? value, ValidateContext context, string displayName)
    {
        var originalDisplayName = context.ValidationContext.DisplayName;
        var originalMemberName = context.ValidationContext.MemberName;

        try
        {
            context.ValidationContext.DisplayName = displayName;
            context.ValidationContext.MemberName = null;

            context.ValidateAllAttributesSynchronously(value, value, this);
        }
        finally
        {
            context.ValidationContext.DisplayName = originalDisplayName;
            context.ValidationContext.MemberName = originalMemberName;
        }
    }

    private async Task ValidateValidatableObjectInterfaceAsync(object? value, ValidateContext context, string displayName, CancellationToken cancellationToken)
    {
        if (Type.ImplementsInterface(typeof(IValidatableObject)) && value is IValidatableObject validatable)
        {
            // Important: Set the DisplayName to the type's resolved display name for top-level
            // validations, and restore the original validation context properties when done.
            var originalDisplayName = context.ValidationContext.DisplayName;
            var originalMemberName = context.ValidationContext.MemberName;
            var errorPrefix = context.CurrentValidationPath;

            try
            {
                context.ValidationContext.DisplayName = displayName;
                context.ValidationContext.MemberName = null;

                if (value is IAsyncValidatableObject asyncValidatable)
                {
                    await foreach (var validationResult in asyncValidatable.ValidateAsync(context.ValidationContext, cancellationToken))
                    {
                        HandleValidationResultForValidatableObject(validationResult, errorPrefix, value, context);
                    }
                }
                else
                {
                    foreach (var validationResult in validatable.Validate(context.ValidationContext))
                    {
                        HandleValidationResultForValidatableObject(validationResult, errorPrefix, value, context);
                    }
                }
            }
            finally
            {
                // Restore the original validation context properties
                context.ValidationContext.DisplayName = originalDisplayName;
                context.ValidationContext.MemberName = originalMemberName;
            }
        }
    }

    private static void HandleValidationResultForValidatableObject(ValidationResult validationResult, string errorPrefix, object? value, ValidateContext context)
    {
        if (validationResult != ValidationResult.Success && validationResult.ErrorMessage is not null)
        {
            // Create a validation error for each member name that is provided
            // We don't support automatic localization of IValidatableObject messages
            foreach (var memberName in validationResult.MemberNames)
            {
                var key = string.IsNullOrEmpty(errorPrefix) ? memberName : $"{errorPrefix}.{memberName}";
                var errorContext = new ValidationErrorContext()
                {
                    Name = memberName,
                    Path = key,
                    Errors = [validationResult.ErrorMessage],
                    Container = value,
                };
                context.AddValidationError(errorContext);
            }

            if (!validationResult.MemberNames.Any())
            {
                // If no member names are specified, then treat this as a top-level error
                var errorContext = new ValidationErrorContext()
                {
                    Name = string.Empty,
                    Path = string.Empty,
                    Errors = [validationResult.ErrorMessage],
                    Container = value,
                };
                context.AddValidationError(errorContext);
            }
        }
    }

    private void ValidateValidatableObjectInterface(object? value, ValidateContext context, string displayName)
    {
        if (Type.ImplementsInterface(typeof(IValidatableObject)) && value is IValidatableObject validatable)
        {
            // Important: Set the DisplayName to the type's resolved display name for top-level
            // validations, and restore the original validation context properties when done.
            var originalDisplayName = context.ValidationContext.DisplayName;
            var originalMemberName = context.ValidationContext.MemberName;
            var errorPrefix = context.CurrentValidationPath;

            try
            {
                context.ValidationContext.DisplayName = displayName;
                context.ValidationContext.MemberName = null;

                foreach (var validationResult in validatable.Validate(context.ValidationContext))
                {
                    HandleValidationResultForValidatableObject(validationResult, errorPrefix, value, context);
                }
            }
            finally
            {
                // Restore the original validation context properties
                context.ValidationContext.DisplayName = originalDisplayName;
                context.ValidationContext.MemberName = originalMemberName;
            }
        }
    }

    private IEnumerable<ValidatableTypeInfo> GetSuperTypeInfos(Type actualType, ValidationOptions options)
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

        static ValidatableTypeInfo? TryGetValidatableTypeInfo(Type superType, Type actualType, ValidationOptions options)
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

    ValidationAttribute[] IValidationErrorReporter.GetValidationAttributes()
    {
        return GetValidationAttributes();
    }

    void IValidationErrorReporter.ReportError(ValidateContext context, object? container, ValidationAttribute attribute, ValidationResult result)
    {
        foreach (var memberName in result.MemberNames)
        {
            // Create a validation error for each member name that is provided
            var errorMessage = context.ResolveAttributeErrorMessage(
                memberName,
                context.ValidationContext.DisplayName,
                declaringType: Type,
                attribute,
                result);

            if (errorMessage is not null)
            {
                var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? memberName : $"{context.CurrentValidationPath}.{memberName}";
                var errorContext = new ValidationErrorContext()
                {
                    Name = memberName,
                    Path = key,
                    Errors = [errorMessage],
                    Container = container,
                };
                context.AddValidationError(errorContext);
            }
        }

        if (!result.MemberNames.Any())
        {
            // If no member names are specified, then treat this as a top-level error
            var errorMessage = context.ResolveAttributeErrorMessage(
                memberName: Type.Name,
                context.ValidationContext.DisplayName,
                declaringType: Type,
                attribute,
                result);

            if (errorMessage is not null)
            {
                var errorContext = new ValidationErrorContext()
                {
                    Name = string.Empty,
                    Path = context.CurrentValidationPath,
                    Errors = [errorMessage],
                    Container = container,
                };
                context.AddValidationError(errorContext);
            }
        }
    }
}
