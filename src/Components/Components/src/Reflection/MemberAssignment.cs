// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Reflection
{
    internal class MemberAssignment
    {
        public static PropertyEnumerable GetPropertiesIncludingInherited(
            [DynamicallyAccessedMembers(Component)] Type type,
            BindingFlags bindingFlags)
        {
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);

            Type? currentType = type;

            while (currentType != null)
            {
                var properties = currentType.GetProperties(bindingFlags | BindingFlags.DeclaredOnly);
                foreach (var property in properties)
                {
                    if (!dictionary.TryGetValue(property.Name, out var others))
                    {
                        dictionary.Add(property.Name, property);
                    }
                    else if (!IsInheritedProperty(property, others))
                    {
                        List<PropertyInfo> many;
                        if (others is PropertyInfo single)
                        {
                            many = new List<PropertyInfo> { single };
                            dictionary[property.Name] = many;
                        }
                        else
                        {
                            many = (List<PropertyInfo>)others;
                        }
                        many.Add(property);
                    }
                }

                currentType = currentType.BaseType;
            }

            return new PropertyEnumerable(dictionary);
        }

        private static bool IsInheritedProperty(PropertyInfo property, object others)
        {
            if (others is PropertyInfo single)
            {
                return single.GetMethod?.GetBaseDefinition() == property.GetMethod?.GetBaseDefinition();
            }

            var many = (List<PropertyInfo>)others;
            foreach (var other in CollectionsMarshal.AsSpan(many))
            {
                if (other.GetMethod?.GetBaseDefinition() == property.GetMethod?.GetBaseDefinition())
                {
                    return true;
                }
            }

            return false;
        }

        public ref struct PropertyEnumerable
        {
            private readonly PropertyEnumerator _enumerator;

            public PropertyEnumerable(Dictionary<string, object> dictionary)
            {
                _enumerator = new PropertyEnumerator(dictionary);
            }

            public PropertyEnumerator GetEnumerator() => _enumerator;
        }

        public ref struct PropertyEnumerator
        {
            // Do NOT make this readonly, or MoveNext will not work
            private Dictionary<string, object>.Enumerator _dictionaryEnumerator;
            private Span<PropertyInfo>.Enumerator _spanEnumerator;

            public PropertyEnumerator(Dictionary<string, object> dictionary)
            {
                _dictionaryEnumerator = dictionary.GetEnumerator();
                _spanEnumerator = Span<PropertyInfo>.Empty.GetEnumerator();
            }

            public PropertyInfo Current
            {
                get
                {
                    if (_dictionaryEnumerator.Current.Value is PropertyInfo property)
                    {
                        return property;
                    }

                    return _spanEnumerator.Current;
                }
            }

            public bool MoveNext()
            {
                if (_spanEnumerator.MoveNext())
                {
                    return true;
                }

                if (!_dictionaryEnumerator.MoveNext())
                {
                    return false;
                }

                var oneOrMoreProperties = _dictionaryEnumerator.Current.Value;
                if (oneOrMoreProperties is PropertyInfo)
                {
                    _spanEnumerator = Span<PropertyInfo>.Empty.GetEnumerator();
                    return true;
                }

                var many = (List<PropertyInfo>)oneOrMoreProperties;
                _spanEnumerator = CollectionsMarshal.AsSpan(many).GetEnumerator();
                var moveNext = _spanEnumerator.MoveNext();
                Debug.Assert(moveNext, "We expect this to at least have one item.");
                return moveNext;
            }
        }
    }
}
