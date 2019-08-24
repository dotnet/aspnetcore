// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Reflection
{
    internal class MemberAssignment
    {
        public static IEnumerable<PropertyInfo> GetPropertiesIncludingInherited(
            Type type, BindingFlags bindingFlags)
        {
            while (type != null)
            {
                var properties = type.GetProperties(bindingFlags)
                    .Where(prop => prop.DeclaringType == type);
                foreach (var property in properties)
                {
                    yield return property;
                }

                type = type.BaseType;
            }
        }

        public static IPropertySetter CreatePropertySetter(Type targetType, PropertyInfo property, bool cascading)
        {
            if (property.SetMethod == null)
            {
                throw new InvalidOperationException($"Cannot provide a value for property " +
                    $"'{property.Name}' on type '{targetType.FullName}' because the property " +
                    $"has no setter.");
            }

            return (IPropertySetter)Activator.CreateInstance(
                typeof(PropertySetter<,>).MakeGenericType(targetType, property.PropertyType),
                property.SetMethod,
                cascading);
        }

        class PropertySetter<TTarget, TValue> : IPropertySetter
        {
            private readonly Action<TTarget, TValue> _setterDelegate;

            public PropertySetter(MethodInfo setMethod, bool cascading)
            {
                _setterDelegate = (Action<TTarget, TValue>)Delegate.CreateDelegate(
                    typeof(Action<TTarget, TValue>), setMethod);
                Cascading = cascading;
            }

            public bool Cascading { get; }

            public void SetValue(object target, object value)
            {
                if (value == null)
                {
                    _setterDelegate((TTarget)target, default);
                }
                else
                {
                    _setterDelegate((TTarget)target, (TValue)value);
                }
            }
        }
    }
}
