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
public abstract class ValidatableTypeInfo : IValidatableInfo
{
    private readonly int _membersCount;
    private readonly List<Type> _superTypes;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatableTypeInfo"/>.
    /// </summary>
    /// <param name="type">The type being validated.</param>
    /// <param name="members">The members that can be validated.</param>
    /// <param name="displayNameInfo">An optional <see cref="DisplayNameInfo"/> that resolves the
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
        _superTypes = type.GetAllImplementedTypes();
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
    /// <paramref name="memberName"/>, including members inherited from base types or implemented
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
    /// <paramref name="options"/>'s <see cref="ValidationOptions.Resolvers"/>. Super-types that
    /// are not registered with a resolver are silently skipped.
    /// </para>
    /// </remarks>
    /// <param name="memberName">The CLR name of the member to find.</param>
    /// <param name="options">The <see cref="ValidationOptions"/> used to resolve metadata for super-types.</param>
    /// <returns>The matching <see cref="ValidatablePropertyInfo"/>, or <see langword="null"/> if no
    /// member with the specified name is declared on <see cref="Type"/> or any of its super-types.</returns>
    internal ValidatablePropertyInfo? FindMember(string memberName, ValidationOptions options)
    {
        if (FindLocalMember(memberName) is { } localMember)
        {
            return localMember;
        }

        foreach (var superType in _superTypes)
        {
            if (options.TryGetValidatableTypeInfo(superType, out var superInfo)
                && superInfo is ValidatableTypeInfo superTypeInfo
                && superTypeInfo.FindLocalMember(memberName) is { } inheritedMember)
            {
                return inheritedMember;
            }
        }

        return null;
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

    /// <inheritdoc />
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            return;
        }

        // Check if we've exceeded the maximum depth
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{Type.Name}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }

        context.ValidationInitiator ??= this;

        var originalPrefix = context.CurrentValidationPath;
        var originalErrorCount = context.ValidationErrors?.Count ?? 0;

        try
        {
            // This allows us to add validation tasks in this list locally (i.e, not the one in ValidateContext).
            // Then, we can do .ContinueWith on that task.
            List<Task>? localValidationTasks = null;

            // First validate direct members
            ValidateMembers(value, context, ref localValidationTasks, cancellationToken);

            var actualType = value.GetType();

            // Then validate inherited members
            foreach (var superTypeInfo in GetSuperTypeInfos(actualType, context))
            {
                superTypeInfo.ValidateMembers(value, context, ref localValidationTasks, cancellationToken);
            }

            // If any property-level validation errors were found, return early
            if (context.ValidationErrors is not null && context.ValidationErrors.Count > originalErrorCount)
            {
                return;
            }

            var displayName = DisplayNameInfo?.GetDisplayName(context, Type.Name, Type) ?? Type.Name;

            // Validate type-level attributes
            var typeValidationTask = ValidateTypeAttributesAsync(value, context, displayName, cancellationToken);
            if (!typeValidationTask.IsCompleted)
            {
                localValidationTasks ??= new();
                localValidationTasks.Add(typeValidationTask);
            }

            // If any type-level attribute errors were found, return early
            if (context.ValidationErrors is not null && context.ValidationErrors.Count > originalErrorCount)
            {
                if (localValidationTasks is not null)
                {
                    context.ValidationTasks.Add(Task.WhenAll(localValidationTasks));
                }

                return;
            }

            // Finally validate IValidatableObject if implemented
            if (localValidationTasks is not null)
            {
                async Task GetFinalValidationTask(List<Task> localValidationTasks)
                {
                    await Task.WhenAll(localValidationTasks);
                    await ValidateValidatableObjectInterfaceAsync(value, context, displayName, cancellationToken);
                }

                context.ValidationTasks.Add(GetFinalValidationTask(localValidationTasks));
            }
            else
            {
                context.ValidationTasks.Add(ValidateValidatableObjectInterfaceAsync(value, context, displayName, cancellationToken));
            }
        }
        finally
        {
            if (context.ValidationInitiator == this)
            {
                await Task.WhenAll(context.ValidationTasks);
            }

            context.CurrentValidationPath = originalPrefix;
        }
    }

    private void ValidateMembers(object? value, ValidateContext context, ref List<Task>? localValidationTasks, CancellationToken cancellationToken)
    {
        for (var i = 0; i < _membersCount; i++)
        {
            var task = Members[i].ValidateAsync(value, context.Clone(), cancellationToken);
            if (!task.IsCompleted)
            {
                localValidationTasks ??= new();
                localValidationTasks.Add(task);
            }
        }
    }

    private async Task ValidateTypeAttributesAsync(object? value, ValidateContext context, string displayName, CancellationToken cancellationToken)
    {
        var validationAttributes = GetValidationAttributes();
        var errorPrefix = context.CurrentValidationPath;

        var originalDisplayName = context.ValidationContext.DisplayName;
        var originalMemberName = context.ValidationContext.MemberName;

        try
        {
            context.ValidationContext.DisplayName = displayName;
            context.ValidationContext.MemberName = null;

            await ValidationHelpers.ValidateAttributesAsync(validationAttributes, value, context, (displayName, Type, errorPrefix, value),
                onValidationError: static (context, result, attribute, state) =>
                {
                    var (displayName, type, errorPrefix, value) = state;
                    foreach (var memberName in result.MemberNames)
                    {
                        // Create a validation error for each member name that is provided
                        var errorMessage = context.ResolveAttributeErrorMessage(
                            memberName,
                            displayName,
                            declaringType: type,
                            attribute,
                            result);

                        if (errorMessage is not null)
                        {
                            var key = string.IsNullOrEmpty(errorPrefix) ? memberName : $"{errorPrefix}.{memberName}";
                            var errorContext = new ValidationErrorContext()
                            {
                                Name = memberName,
                                Path = key,
                                Errors = [errorMessage],
                                Container = value,
                            };
                            context.AddValidationError(errorContext);
                        }
                    }

                    if (!result.MemberNames.Any())
                    {
                        // If no member names are specified, then treat this as a top-level error
                        var errorMessage = context.ResolveAttributeErrorMessage(
                            memberName: type.Name,
                            displayName,
                            declaringType: type,
                            attribute,
                            result);

                        if (errorMessage is not null)
                        {
                            var errorContext = new ValidationErrorContext()
                            {
                                Name = string.Empty,
                                Path = errorPrefix,
                                Errors = [errorMessage],
                                Container = value,
                            };
                            context.AddValidationError(errorContext);
                        }
                    }
                },
                cancellationToken);
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
                        HandleValidationResult(validationResult);
                    }
                }
                else
                {
                    foreach (var validationResult in validatable.Validate(context.ValidationContext))
                    {
                        HandleValidationResult(validationResult);
                    }
                }
            }
            finally
            {
                // Restore the original validation context properties
                context.ValidationContext.DisplayName = originalDisplayName;
                context.ValidationContext.MemberName = originalMemberName;
            }

            void HandleValidationResult(ValidationResult validationResult)
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
        }
    }

    private IEnumerable<ValidatableTypeInfo> GetSuperTypeInfos(Type actualType, ValidateContext context)
    {
        foreach (var superType in _superTypes.Where(t => t.IsAssignableFrom(actualType)))
        {
            if (context.ValidationOptions.TryGetValidatableTypeInfo(superType, out var found)
                && found is ValidatableTypeInfo superTypeInfo)
            {
                yield return superTypeInfo;
            }
        }
    }
}
