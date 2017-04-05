// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public struct TempDataProperty
    {
        private Func<object, object> _getter;

        private Action<object, object> _setter;

        public TempDataProperty(PropertyInfo propertyInfo, Func<object, object> getter, Action<object, object> setter)
        {
            PropertyInfo = propertyInfo;
            _getter = getter;
            _setter = setter;
        }

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
