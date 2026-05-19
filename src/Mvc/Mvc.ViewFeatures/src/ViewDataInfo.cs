// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Represents info about a <see cref="ViewDataDictionary"/> lookup which is evaluated when <see cref="Value"/> is read.
/// </summary>
public class ViewDataInfo
{
    private static readonly Func<object> _propertyInfoResolver = () => null;

    private object _value;
    private Func<object> _valueAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataInfo"/> class with info about a
    /// <see cref="ViewDataDictionary"/> lookup which has already been evaluated.
    /// </summary>
    /// <param name="container">The <see cref="object"/> that <paramref name="value"/> was evaluated from.</param>
    /// <param name="value">The evaluated value.</param>
    public ViewDataInfo(object container, object value)
    {
        Container = container;
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataInfo"/> class with info about a
    /// <see cref="ViewDataDictionary"/> lookup which is evaluated when <see cref="Value"/> is read.
    /// It uses <see cref="System.Reflection.PropertyInfo.GetValue(object)"/> on <paramref name="propertyInfo"/>
    /// passing parameter <paramref name="container"/> to lazily evaluate the value.
    /// </summary>
    /// <param name="container">The <see cref="object"/> that <see cref="Value"/> will be evaluated from.</param>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/> that will be used to evaluate <see cref="Value"/>.</param>
    public ViewDataInfo(object container, PropertyInfo propertyInfo)
        : this(container, propertyInfo, _propertyInfoResolver)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataInfo"/> class with info about a
    /// <see cref="ViewDataDictionary"/> lookup which is evaluated when <see cref="Value"/> is read.
    /// It uses <paramref name="valueAccessor"/> to lazily evaluate the value.
    /// </summary>
    /// <param name="container">The <see cref="object"/> that has the <see cref="Value"/>.</param>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/> that represents <see cref="Value"/>'s property.</param>
    /// <param name="valueAccessor">A delegate that will return the <see cref="Value"/>.</param>
    public ViewDataInfo(object container, PropertyInfo propertyInfo, Func<object> valueAccessor)
    {
        Container = container;
        PropertyInfo = propertyInfo;
        _valueAccessor = valueAccessor;
    }

    /// <summary>
    /// Gets the <see cref="object"/> that has the <see cref="Value"/>.
    /// </summary>
    public object Container { get; }

    /// <summary>
    /// Gets the <see cref="PropertyInfo"/> that represents <see cref="Value"/>'s property.
    /// </summary>
    public PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public object Value
    {
        get
        {
            if (_valueAccessor != null)
            {
                ResolveValue();
            }

            return _value;
        }
        set
        {
            _value = value;
            _valueAccessor = null;
        }
    }

    private void ResolveValue()
    {
        if (ReferenceEquals(_valueAccessor, _propertyInfoResolver))
        {
            _value = PropertyInfo.GetValue(Container);
        }
        else
        {
            _value = _valueAccessor();
        }

        _valueAccessor = null;
    }
}
