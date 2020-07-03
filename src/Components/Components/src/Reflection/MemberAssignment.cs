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
            var dictionary = new Dictionary<string, List<PropertyInfo>>();

            Type? currentType = type;

            while (currentType != null)
            {
                var properties = currentType.GetProperties(bindingFlags)
                    .Where(prop => prop.DeclaringType == currentType);
                foreach (var property in properties)
                {
                    if (!dictionary.TryGetValue(property.Name, out var others))
                    {
                        others = new List<PropertyInfo>();
                        dictionary.Add(property.Name, others);
                    }

                    if (others.Any(other => other.GetMethod?.GetBaseDefinition() == property.GetMethod?.GetBaseDefinition()))
                    {
                        // This is an inheritance case. We can safely ignore the value of property since
                        // we have seen a more derived value.
                        continue;
                    }

                    others.Add(property);
                }

                currentType = currentType.BaseType;
            }

            return dictionary.Values.SelectMany(p => p);
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
                cascading)!;
        }

        class PropertySetter<TTarget, TValue> : IPropertySetter where TTarget : notnull
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
                    _setterDelegate((TTarget)target, default!);
                }
                else
                {
                    _setterDelegate((TTarget)target, (TValue)value);
                }
            }
        }
    }
}
