using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using static Microsoft.AspNetCore.Internal.LinkerFlags;


namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public class DynamicComponentCollection : Collection<DynamicComponentDefinition>
    {
        /// <summary>
        /// Adds a component mapping to the collection.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="elementName">The DOM element selector.</param>
        public DynamicComponentDefinition Register<[DynamicallyAccessedMembers(Component)] TComponent>(string elementName) where TComponent : IComponent
        {
            if (elementName is null)
            {
                throw new ArgumentNullException(nameof(elementName));
            }

            return Register(elementName, typeof(TComponent));
        }

        /// <summary>
        /// Adds a component mapping to the collection.
        /// </summary>
        /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
        /// <param name="elementName">The element name used to refer to this component from JavaScript.</param>
        public DynamicComponentDefinition Register(string elementName, [DynamicallyAccessedMembers(Component)] Type componentType)
        {
            if (componentType is null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (elementName is null)
            {
                throw new ArgumentNullException(nameof(elementName));
            }

            if (!elementName.Contains('-'))
            {
                // We want these names to be potentially used as custom element names in the future, so we want to impose
                // the same naming restrictions on them.
                throw new InvalidOperationException($"'{elementName}' must contain at least a `-` symbol.");
            }

            for (var i = 0; i < Count; i++)
            {
                var element = this[i];
                // This is inline with the way HTML elements work (which is case insensitive). We want to prevent
                // SOME-element and some-element to represent different elements.
                if (string.Equals(element.Name, elementName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"An element with name '{elementName}' is already registered.");
                }
            }

            var discoveredProperties = ComponentProperties.GetParameters(componentType, out var hasCatchAll, out var hasCallbacks);
            var definition = new DynamicComponentDefinition(elementName, componentType)
            {
                HasCallbackParameter = hasCallbacks,
                HasCatchAllProperty = hasCatchAll
            };

            foreach (var property in discoveredProperties)
            {
                definition.Parameters.Add(property);
            }

            Add(definition);
            return definition;
        }

        internal DynamicComponentDefinition? GetByName(string? alias)
        {
            foreach (var definition in this)
            {
                if (string.Equals(alias, definition.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return definition;
                }
            }

            return null;
        }

        // This code is based on the code that ParameterView.SetParameters creates
        // TODO: Refactor this so that we can reuse it to build the parameter definitions
        internal static class ComponentProperties
        {
            private const BindingFlags _bindablePropertyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

            public static IEnumerable<DynamicComponentParameter> GetParameters(Type targetType, out bool catchAll, out bool hasCallbacks)
            {
                if (targetType == null)
                {
                    throw new ArgumentNullException(nameof(targetType));
                }

                catchAll = false;
                hasCallbacks = false;
                var properties = new PropertiesEnumerator(targetType);
                var definitions = new List<DynamicComponentParameter>();

                foreach (var property in properties.GetProperties())
                {
                    if (property.IsCaptureUnmatchedValues)
                    {
                        catchAll = true;
                    }

                    if (property.IsEventCallbackOrDelegate)
                    {
                        hasCallbacks = true;
                    }

                    definitions.Add(new DynamicComponentParameter(property.Name, property.Type)
                    {
                        IsCallback = property.IsEventCallbackOrDelegate
                    });
                }

                return definitions;
            }

            private readonly struct ComponentProperty
            {
                public ComponentProperty(bool isCaptureUnmatchedValues, bool isEventCallbackOrDelegate, string name, Type type)
                {
                    IsCaptureUnmatchedValues = isCaptureUnmatchedValues;
                    IsEventCallbackOrDelegate = isEventCallbackOrDelegate;
                    Name = name;
                    Type = type;
                }

                public bool IsCaptureUnmatchedValues { get; }

                public bool IsEventCallbackOrDelegate { get; }

                public string Name { get; }

                public Type Type { get; }
            }

            private class PropertiesEnumerator
            {
                private readonly Dictionary<string, object> properties = new(StringComparer.Ordinal);
                private readonly Type _targetType;

                public PropertiesEnumerator([DynamicallyAccessedMembers(Component)] Type targetType)
                {
                    properties = GetPropertiesIncludingInherited(targetType, _bindablePropertyFlags);
                    _targetType = targetType;
                }

                public IEnumerable<ComponentProperty> GetProperties()
                {
                    var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in properties)
                    {
                        if (item.Value is PropertyInfo property)
                        {
                            if (IsParameter(property))
                            {
                                yield return CreateProperty(_targetType, property, propertyNames);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            var list = (List<PropertyInfo>)item.Value;
                            var count = list.Count;
                            for (var i = 0; i < count; i++)
                            {
                                property = list[i];
                                if (IsParameter(property))
                                {
                                    yield return CreateProperty(_targetType, property, propertyNames);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    bool IsParameter(PropertyInfo propertyInfo)
                    {
                        var parameterAttribute = propertyInfo.GetCustomAttribute<ParameterAttribute>();
                        var cascadingParameterAttribute = propertyInfo.GetCustomAttribute<CascadingParameterAttribute>();
                        return parameterAttribute != null || cascadingParameterAttribute != null;
                    }

                    ComponentProperty CreateProperty(Type targetType, PropertyInfo propertyInfo, HashSet<string> propertyNames)
                    {
                        var parameterAttribute = propertyInfo.GetCustomAttribute<ParameterAttribute>();
                        var cascadingParameterAttribute = propertyInfo.GetCustomAttribute<CascadingParameterAttribute>();

                        var propertyName = propertyInfo.Name;
                        if (parameterAttribute != null && (propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic))
                        {
                            throw new InvalidOperationException(
                                $"The type '{targetType.FullName}' declares a parameter matching the name '{propertyName}' that is not public. Parameters must be public.");
                        }

                        if (propertyNames.Contains(propertyName))
                        {
                            throw new InvalidOperationException(
                                $"The type '{targetType.FullName}' declares more than one parameter matching the " +
                                $"name '{propertyName.ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.");
                        }

                        propertyNames.Add(propertyName);

                        return new ComponentProperty(
                            parameterAttribute?.CaptureUnmatchedValues == true,
                            propertyInfo.PropertyType.IsAssignableTo(typeof(Delegate)) ||
                            propertyInfo.PropertyType.IsAssignableTo(typeof(EventCallback)) ||
                            (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition().IsAssignableTo(typeof(EventCallback<>))),
                            propertyInfo.Name,
                            propertyInfo.PropertyType);
                    }
                }

                private static Dictionary<string, object> GetPropertiesIncludingInherited(
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

                    return dictionary;
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
            }
        }
    }
}
