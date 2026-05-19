// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.Internal;

internal sealed class PropertyActivator<TContext>
{
    private readonly Func<TContext, object> _valueAccessor;
    private readonly Action<object, object> _fastPropertySetter;

    public PropertyActivator(
        PropertyInfo propertyInfo,
        Func<TContext, object> valueAccessor)
    {
        PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        _valueAccessor = valueAccessor ?? throw new ArgumentNullException(nameof(valueAccessor));
        _fastPropertySetter = PropertyHelper.MakeFastPropertySetter(propertyInfo);
    }

    public PropertyInfo PropertyInfo { get; private set; }

    public object Activate(object instance, TContext context)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var value = _valueAccessor(context);
        _fastPropertySetter(instance, value);
        return value;
    }

    public static PropertyActivator<TContext>[] GetPropertiesToActivate(
        Type type,
        Type activateAttributeType,
        Func<PropertyInfo, PropertyActivator<TContext>> createActivateInfo)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(activateAttributeType);
        ArgumentNullException.ThrowIfNull(createActivateInfo);

        return GetPropertiesToActivate(type, activateAttributeType, createActivateInfo, includeNonPublic: false);
    }

    public static PropertyActivator<TContext>[] GetPropertiesToActivate(
        Type type,
        Type activateAttributeType,
        Func<PropertyInfo, PropertyActivator<TContext>> createActivateInfo,
        bool includeNonPublic)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(activateAttributeType);
        ArgumentNullException.ThrowIfNull(createActivateInfo);

        var properties = GetActivatableProperties(type, activateAttributeType, includeNonPublic);
        return properties.Select(createActivateInfo).ToArray();
    }

    public static PropertyActivator<TContext>[] GetPropertiesToActivate<TAttribute>(
        Type type,
        Func<PropertyInfo, TAttribute, PropertyActivator<TContext>> createActivateInfo,
        bool includeNonPublic)
        where TAttribute : Attribute
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(createActivateInfo);

        var properties = GetActivatableProperties(type, typeof(TAttribute), includeNonPublic);
        return properties.Select(property =>
        {
            var attribute = property.GetCustomAttribute<TAttribute>()!;
            return createActivateInfo(property, attribute);
        }).ToArray();
    }

    private static IEnumerable<PropertyInfo> GetActivatableProperties(
        Type type,
        Type activateAttributeType,
        bool includeNonPublic)
    {
        var properties = type.GetRuntimeProperties()
            .Where((property) =>
            {
                return
                    property.IsDefined(activateAttributeType) &&
                    property.GetIndexParameters().Length == 0 &&
                    property.SetMethod != null &&
                    !property.SetMethod.IsStatic;
            });

        if (!includeNonPublic)
        {
            properties = properties.Where(property => property.SetMethod is { IsPublic: true });
        }

        return properties;
    }
}
