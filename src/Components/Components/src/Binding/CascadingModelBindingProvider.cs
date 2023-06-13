// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// Provides values that get supplied to cascading parameters with <see cref="CascadingModelBinder"/>.
/// </summary>
public abstract class CascadingModelBindingProvider
{
    /// <summary>
    /// Gets whether values supplied by this instance will not change.
    /// </summary>
    protected internal abstract bool AreValuesFixed { get; }

    /// <summary>
    /// Determines whether this instance can provide values for parameters annotated with the specified attribute type.
    /// </summary>
    /// <param name="attributeType">The attribute type.</param>
    /// <returns><c>true</c> if this instance can provide values for parameters annotated with the specified attribute type, otherwise <c>false</c>.</returns>
    protected internal abstract bool SupportsCascadingParameterAttributeType(Type attributeType);

    /// <summary>
    /// Determines whether this instance can provide values to parameters with the specified type.
    /// </summary>
    /// <param name="parameterType">The parameter type.</param>
    /// <returns><c>true</c> if this instance can provide values to parameters with the specified type, otherwise <c>false</c>.</returns>
    protected internal abstract bool SupportsParameterType(Type parameterType);

    /// <summary>
    /// Determines whether this instance can supply a value for the specified parameter.
    /// </summary>
    /// <param name="bindingContext">The current <see cref="ModelBindingContext"/>.</param>
    /// <param name="parameterInfo">The <see cref="CascadingParameterInfo"/> for the component parameter.</param>
    /// <returns><c>true</c> if a value can be supplied, otherwise <c>false</c>.</returns>
    protected internal abstract bool CanSupplyValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo);

    /// <summary>
    /// Gets the value for the specified parameter.
    /// </summary>
    /// <param name="bindingContext">The current <see cref="ModelBindingContext"/>.</param>
    /// <param name="parameterInfo">The <see cref="CascadingParameterInfo"/> for the component parameter.</param>
    /// <returns>The value to supply to the parameter.</returns>
    protected internal abstract object? GetCurrentValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo);

    /// <summary>
    /// Subscribes to changes in supplied values, if they can change.
    /// </summary>
    /// <remarks>
    /// This method must be implemented if <see cref="AreValuesFixed"/> is <c>false</c>.
    /// </remarks>
    /// <param name="subscriber">The <see cref="ComponentState"/> for the subscribing component.</param>
    protected internal virtual void Subscribe(ComponentState subscriber)
        => throw new InvalidOperationException(
            $"'{nameof(CascadingModelBindingProvider)}' instances that have non-fixed values must provide an implementation for '{nameof(Subscribe)}'.");

    /// <summary>
    /// Unsubscribes from changes in supplied values, if they can change.
    /// </summary>
    /// <remarks>
    /// This method must be implemented if <see cref="AreValuesFixed"/> is <c>false</c>.
    /// </remarks>
    /// <param name="subscriber">The <see cref="ComponentState"/> for the unsubscribing component.</param>
    protected internal virtual void Unsubscribe(ComponentState subscriber)
        => throw new InvalidOperationException(
            $"'{nameof(CascadingModelBindingProvider)}' instances that have non-fixed values must provide an implementation for '{nameof(Unsubscribe)}'.");
}
