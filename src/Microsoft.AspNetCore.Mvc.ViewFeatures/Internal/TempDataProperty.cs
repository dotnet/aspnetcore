// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public struct TempDataProperty
    {
        private readonly Func<object, object> _getter;

        private readonly Action<object, object> _setter;

        public TempDataProperty(string tempDataKey, PropertyInfo propertyInfo, Func<object, object> getter, Action<object, object> setter)
        {
            TempDataKey = tempDataKey;
            PropertyInfo = propertyInfo;
            _getter = getter;
            _setter = setter;
        }

        public string TempDataKey { get; }

        public PropertyInfo PropertyInfo { get; }

        public object GetValue(object obj)
        {
            return _getter(obj);
        }

        public void SetValue(object obj, object value)
        {
            _setter(obj, value);
        }
    }
}
