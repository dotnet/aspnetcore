// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
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
}
