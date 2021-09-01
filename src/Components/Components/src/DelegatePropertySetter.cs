// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Sets a component property using a delegate.
/// </summary>
/// <typeparam name="TComponent">The type of the component being acted on.</typeparam>
public class DelegatePropertySetter<TComponent> : IPropertySetter
{
    private readonly Action<TComponent, object> _propertySetterDelegate;

    /// <inheritdoc/>
    public bool Cascading { get; }

    /// <summary>
    /// Constructs an instance of <see cref="DelegatePropertySetter{TComponent}"/>.
    /// </summary>
    /// <param name="propertySetterDelegate">The delegate used to set the property.</param>
    /// <param name="cascading">Whether the property is cascading.</param>
    public DelegatePropertySetter(Action<TComponent, object> propertySetterDelegate, bool cascading = false)
    {
        _propertySetterDelegate = propertySetterDelegate;

        Cascading = cascading;
    }

    /// <inheritdoc/>
    public void SetValue(object target, object value)
        => _propertySetterDelegate((TComponent)target, value);
}

/// <summary>
/// Sets the component property capturing unmatched values using a delegate.
/// </summary>
/// <typeparam name="TComponent">The type of the component being acted on.</typeparam>
public sealed class UnmatchedValuesDelegatePropertySetter<TComponent> : DelegatePropertySetter<TComponent>, IUnmatchedValuesPropertySetter
{
    /// <inheritdoc/>
    public string UnmatchedValuesPropertyName { get; }

    /// <summary>
    /// Constructs an instance of <see cref="UnmatchedValuesDelegatePropertySetter{TComponent}"/>.
    /// </summary>
    /// <param name="unmatchedValuesPropertyName">The name of the component property that this instance sets.</param>
    /// <param name="propertySetter">The delegate used to set the property.</param>
    public UnmatchedValuesDelegatePropertySetter(string unmatchedValuesPropertyName, Action<TComponent, object> propertySetter)
        : base(propertySetter)
    {
        UnmatchedValuesPropertyName = unmatchedValuesPropertyName;
    }
}
