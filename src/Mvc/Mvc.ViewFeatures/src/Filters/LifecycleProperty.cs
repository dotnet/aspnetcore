// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

[DebuggerDisplay("{PropertyInfo, nq}")]
internal readonly struct LifecycleProperty
{
    private readonly PropertyHelper _propertyHelper;
    private readonly bool _isReferenceTypeOrNullable;

    public LifecycleProperty(PropertyInfo propertyInfo, string key)
    {
        Key = key;
        _propertyHelper = new PropertyHelper(propertyInfo);
        var propertyType = propertyInfo.PropertyType;
        _isReferenceTypeOrNullable = !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null;
    }

    public string Key { get; }

    public PropertyInfo PropertyInfo => _propertyHelper.Property;

    public object GetValue(object instance) => _propertyHelper.GetValue(instance);

    public void SetValue(object instance, object value)
    {
        if (value != null || _isReferenceTypeOrNullable)
        {
            _propertyHelper.SetValue(instance, value);
        }
    }
}
