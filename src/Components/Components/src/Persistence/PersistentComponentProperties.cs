// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Reflection;

internal static class PersistentComponentProperties
{
    internal const BindingFlags BindablePropertyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

    // Right now it's not possible for a component to define a Parameter and a Cascading Parameter with
    // the same name. We don't give you a way to express this in code (would create duplicate properties),
    // and we don't have the ability to represent it in our data structures.
    private static readonly ConcurrentDictionary<Type, PersistentPropertiesForType> _cachedWritersByType
        = new ConcurrentDictionary<Type, PersistentPropertiesForType>();

    public static void ClearCache() => _cachedWritersByType.Clear();

    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.Reflection.PersistentComponentProperties.PersistentPropertiesForType.RestoreProperties(IPersistentComponentState, Object)")]
    public static void RestoreProperties(IPersistentComponentState componentState, object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var targetType = GetType(target);
        if (!_cachedWritersByType.TryGetValue(targetType, out var writers))
        {
            writers = new PersistentPropertiesForType(targetType);
            _cachedWritersByType[targetType] = writers;
        }

        writers.RestoreProperties(componentState, target);
    }

    [return: DynamicallyAccessedMembers(Component)]
    [UnconditionalSuppressMessage(
            "ReflectionAnalysis",
            "IL2073:Microsoft.AspNetCore.Components.Reflection.PersistentComponentProperties.GetType(Object)",
            Justification = "The referenced methods don't have any DynamicallyAccessedMembers annotations. See https://github.com/mono/linker/issues/1727")]
    private static Type GetType(object target) => target.GetType();

    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.Reflection.PersistentComponentProperties.PersistentPropertiesForType.PersistProperties(IPersistentComponentState, Object)")]
    public static void PersistProperties(IPersistentComponentState componentState, object target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var targetType = GetType(target);
        if (!_cachedWritersByType.TryGetValue(targetType, out var writers))
        {
            writers = new PersistentPropertiesForType(targetType);
            _cachedWritersByType[targetType] = writers;
        }

        writers.PersistProperties(componentState, target);
    }

    internal static IEnumerable<PropertyInfo> GetCandidateBindableProperties([DynamicallyAccessedMembers(Component)] Type targetType)
        => MemberAssignment.GetPropertiesIncludingInherited(targetType, BindablePropertyFlags);

    private sealed class PersistentPropertiesForType
    {
        private readonly PersistentProperty[] _underlyingPersistorRestorers;

        public PersistentPropertiesForType([DynamicallyAccessedMembers(Component)] Type targetType)
        {
            var underlyingWriters = new Dictionary<string, PersistentProperty>(StringComparer.OrdinalIgnoreCase);

            foreach (var propertyInfo in GetCandidateBindableProperties(targetType))
            {
                var persistedAttribute = propertyInfo.GetCustomAttribute<PersistedAttribute>();
                if (persistedAttribute == null)
                {
                    continue;
                }

                var propertyName = propertyInfo.Name;
                if (persistedAttribute != null && (propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic))
                {
                    throw new InvalidOperationException(
                        $"The type '{targetType.FullName}' declares a parameter matching the name '{propertyName}' that is not public. Parameters must be public.");
                }

                var propertyRestorer = new PersistentProperty(targetType, propertyInfo);

                if (underlyingWriters.ContainsKey(propertyName))
                {
                    throw new InvalidOperationException(
                        $"The type '{targetType.FullName}' declares more than one parameter matching the " +
                        $"name '{propertyName.ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.");
                }

                underlyingWriters.Add(propertyName, propertyRestorer);
            }

            _underlyingPersistorRestorers = underlyingWriters.Values.ToArray();
        }

        [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.Reflection.PersistentComponentProperties.PersistentProperty.RestoreValue(IPersistentComponentState, Object)")]
        public void RestoreProperties(IPersistentComponentState state, object target)
        {
            for (var i = 0; i < _underlyingPersistorRestorers.Length; i++)
            {
                var restorer = _underlyingPersistorRestorers[i];
                restorer.RestoreValue(state, target);
            }
        }

        [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.Reflection.PersistentComponentProperties.PersistentProperty.PersistValue(IPersistentComponentState, Object)")]
        public void PersistProperties(IPersistentComponentState state, object target)
        {
            for (var i = 0; i < _underlyingPersistorRestorers.Length; i++)
            {
                var persistor = _underlyingPersistorRestorers[i];
                persistor.PersistValue(state, target);
            }
        }
    }

    internal sealed class PersistentProperty
    {
        private static readonly MethodInfo CallPropertyRestorerOpenGenericMethod =
            typeof(PersistentProperty).GetMethod(nameof(CallPropertyRestorer), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo CallPropertyPersistorOpenGenericMethod =
            typeof(PersistentProperty).GetMethod(nameof(CallPropertyPersistor), BindingFlags.NonPublic | BindingFlags.Static)!;

        private readonly Action<IPersistentComponentState, string, object> _restorerDelegate;
        private readonly Action<IPersistentComponentState, string, object> _persistorDelegate;
        private readonly string _propertyKey;

        [UnconditionalSuppressMessage(
            "ReflectionAnalysis",
            "IL2060:MakeGenericMethod",
            Justification = "The referenced methods don't have any DynamicallyAccessedMembers annotations. See https://github.com/mono/linker/issues/1727")]
        public PersistentProperty(Type targetType, PropertyInfo property)
        {
            if (property.SetMethod == null)
            {
                throw new InvalidOperationException("Cannot provide a value for property " +
                    $"'{property.Name}' on type '{targetType.FullName}' because the property " +
                    "has no setter.");
            }

            if (property.GetMethod == null)
            {
                throw new InvalidOperationException("Cannot provide a value for property " +
                    $"'{property.Name}' on type '{targetType.FullName}' because the property " +
                    "has no getter.");
            }

            // TODO: Produce a key based on a subset of the hash of this info
            _propertyKey = property.DeclaringType!.FullName + "." + property.Name;

            var setMethod = property.SetMethod;

            var propertySetterAsAction =
                setMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(targetType, property.PropertyType));
            var callPropertyDeserializerClosedGenericMethod =
                CallPropertyRestorerOpenGenericMethod.MakeGenericMethod(targetType, property.PropertyType);
            _restorerDelegate = (Action<IPersistentComponentState, string, object>)
                callPropertyDeserializerClosedGenericMethod.CreateDelegate(typeof(Action<IPersistentComponentState, string, object>), propertySetterAsAction);

            var getMethod = property.GetMethod;

            var propertyGetterAsFunction =
                getMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(targetType, property.PropertyType));
            var callPropertySerializerClosedGenericMethod =
                CallPropertyPersistorOpenGenericMethod.MakeGenericMethod(targetType, property.PropertyType);
            _persistorDelegate = (Action<IPersistentComponentState, string, object>)
                callPropertySerializerClosedGenericMethod.CreateDelegate(typeof(Action<IPersistentComponentState, string, object>), propertyGetterAsFunction);
        }

        // TODO: Incorporate code to hash in the PersistentKeyProperties
        [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.IPersistentComponentState.TryTakeFromJson<TValue>(String, out TValue)")]
        public void RestoreValue(IPersistentComponentState state, object target)
            => _restorerDelegate(state, _propertyKey, target);

        [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.IPersistentComponentState.TryTakeFromJson<TValue>(String, out TValue)")]
        public void PersistValue(IPersistentComponentState state, object target)
            => _persistorDelegate(state, _propertyKey, target);

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "<Pending>")]
        private static void CallPropertyRestorer<TTarget, [DynamicallyAccessedMembers(JsonSerialized)] TValue>(
            Action<TTarget, TValue> setter,
            IPersistentComponentState state,
            string key,
            object target)
            where TTarget : notnull, new()
        {
            if (state.TryTakeFromJson<TValue>(key, out var value))
            {
                setter((TTarget)target, value!);
            }
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "<Pending>")]
        private static void CallPropertyPersistor<TTarget, [DynamicallyAccessedMembers(JsonSerialized)] TValue>(
            Func<TTarget, TValue> getter,
            IPersistentComponentState state,
            string key,
            object target)
            where TTarget : notnull, new()
        {
            var value = getter((TTarget)target);
            state.PersistAsJson(key, value!);
        }
    }
}
