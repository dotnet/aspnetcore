// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Provider for error messages the model binding system detects.
/// </summary>
public abstract class ModelBindingMessageProvider
{
    /// <summary>
    /// Error message the model binding system adds when a property with an associated
    /// <c>BindRequiredAttribute</c> is not bound.
    /// </summary>
    /// <value>
    /// Default <see cref="string"/> is "A value for the '{0}' parameter or property was not provided.".
    /// </value>
    public virtual Func<string, string> MissingBindRequiredValueAccessor { get; } = default!;

    /// <summary>
    /// Error message the model binding system adds when either the key or the value of a
    /// <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> is bound but not both.
    /// </summary>
    /// <value>Default <see cref="string"/> is "A value is required.".</value>
    public virtual Func<string> MissingKeyOrValueAccessor { get; } = default!;

    /// <summary>
    /// Error message the model binding system adds when no value is provided for the request body,
    /// but a value is required.
    /// </summary>
    /// <value>Default <see cref="string"/> is "A non-empty request body is required.".</value>
    public virtual Func<string> MissingRequestBodyRequiredValueAccessor { get; } = default!;

    /// <summary>
    /// Error message the model binding system adds when a <c>null</c> value is bound to a
    /// non-<see cref="Nullable"/> property.
    /// </summary>
    /// <value>Default <see cref="string"/> is "The value '{0}' is invalid.".</value>
    public virtual Func<string, string> ValueMustNotBeNullAccessor { get; } = default!;

    /// <summary>
    /// Error message the model binding system adds when <see cref="ModelError.Exception"/> is of type
    /// <see cref="FormatException"/> or <see cref="OverflowException"/>, value is known, and error is associated
    /// with a property.
    /// </summary>
    /// <value>Default <see cref="string"/> is "The value '{0}' is not valid for {1}.".</value>
    public virtual Func<string, string, string> AttemptedValueIsInvalidAccessor { get; } = default!;

    /// <summary>
    /// Error message the model binding system adds when <see cref="ModelError.Exception"/> is of type
    /// <see cref="FormatException"/> or <see cref="OverflowException"/>, value is known, and error is associated
    /// with a collection element or parameter.
    /// </summary>
    /// <value>Default <see cref="string"/> is "The value '{0}' is not valid.".</value>
    public virtual Func<string, string> NonPropertyAttemptedValueIsInvalidAccessor { get; } = default!;

    /// <summary>
    /// Error message the model binding system adds when <see cref="ModelError.Exception"/> is of type
    /// <see cref="FormatException"/> or <see cref="OverflowException"/>, value is unknown, and error is associated
    /// with a property.
    /// </summary>
    /// <value>Default <see cref="string"/> is "The supplied value is invalid for {0}.".</value>
    public virtual Func<string, string> UnknownValueIsInvalidAccessor { get; } = default!;

    /// <summary>
    /// Error message the model binding system adds when <see cref="ModelError.Exception"/> is of type
    /// <see cref="FormatException"/> or <see cref="OverflowException"/>, value is unknown, and error is associated
    /// with a collection element or parameter.
    /// </summary>
    /// <value>Default <see cref="string"/> is "The supplied value is invalid.".</value>
    public virtual Func<string> NonPropertyUnknownValueIsInvalidAccessor { get; } = default!;

    /// <summary>
    /// Fallback error message HTML and tag helpers display when a property is invalid but the
    /// <see cref="ModelError"/>s have <c>null</c> <see cref="ModelError.ErrorMessage"/>s.
    /// </summary>
    /// <value>Default <see cref="string"/> is "The value '{0}' is invalid.".</value>
    public virtual Func<string, string> ValueIsInvalidAccessor { get; } = default!;

    /// <summary>
    /// Error message HTML and tag helpers add for client-side validation of numeric formats. Visible in the
    /// browser if the field for a <c>float</c> (for example) property does not have a correctly-formatted value.
    /// </summary>
    /// <value>Default <see cref="string"/> is "The field {0} must be a number.".</value>
    public virtual Func<string, string> ValueMustBeANumberAccessor { get; } = default!;

    /// <summary>
    /// Error message HTML and tag helpers add for client-side validation of numeric formats. Visible in the
    /// browser if the field for a <c>float</c> (for example) collection element or action parameter does not have a
    /// correctly-formatted value.
    /// </summary>
    /// <value>Default <see cref="string"/> is "The field must be a number.".</value>
    public virtual Func<string> NonPropertyValueMustBeANumberAccessor { get; } = default!;
}
