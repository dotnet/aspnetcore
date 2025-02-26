// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

internal class PersistentServicesRegistry
{
    private static readonly string _registryKey = typeof(PersistentServicesRegistry).FullName!;

    private readonly IServiceProvider _serviceProvider;
    private readonly PersistentServiceTypeCache _persistentServiceTypeCache;
    private IEnumerable<IPersistentComponentRegistration> _registrations;
    private PersistingComponentStateSubscription _subscription;
    private static readonly ConcurrentDictionary<Type, PropertiesAccessor> _cachedAccessorsByType = new();

    public PersistentServicesRegistry(
        IServiceProvider serviceProvider,
        IEnumerable<IPersistentComponentRegistration> registrations)
    {
        _serviceProvider = serviceProvider;
        _persistentServiceTypeCache = new PersistentServiceTypeCache();
        _registrations = registrations;
    }

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    private class PersistentComponentRegistration : IPersistentComponentRegistration
    {
        public string Assembly { get; set; } = "";

        public string FullTypeName { get; set; } = "";

        private string GetDebuggerDisplay() => $"{Assembly}::{FullTypeName}";
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private void RestoreRegistrationsIfAvailable(PersistentComponentState state)
    {
        foreach (var registration in _registrations)
        {
            var type = ResolveType(registration.Assembly, registration.FullTypeName);
            if (type == null)
            {
                continue;
            }

            var instance = _serviceProvider.GetService(type);
            if (instance != null)
            {
                RestoreInstanceState(instance, type, state);
            }
        }
    }

    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.PersistentComponentState.TryTakeFromJson(String, Type, out Object)")]
    private static void RestoreInstanceState(object instance, Type type, PersistentComponentState state)
    {
        var accessors = _cachedAccessorsByType.GetOrAdd(instance.GetType(), static (Type runtimeType, Type declaredType) => new PropertiesAccessor(runtimeType, declaredType), type);
        foreach (var (key, propertyType) in accessors.KeyTypePairs)
        {
            if (state.TryTakeFromJson(key, propertyType, out var result))
            {
                var (setter, getter) = accessors.GetAccessor(key);
                setter.SetValue(instance, result!);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private void PersistServicesState(PersistentComponentState state)
    {
        // Persist all the registrations
        state.PersistAsJson(_registryKey, _registrations);
        foreach (var registration in _registrations)
        {
            var type = ResolveType(registration.Assembly, registration.FullTypeName);
            if (type == null)
            {
                continue;
            }

            var instance = _serviceProvider.GetRequiredService(type);
            PersistInstanceState(instance, type, state);
        }
    }

    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.PersistentComponentState.PersistAsJson(String, Object, Type)")]
    private static void PersistInstanceState(object instance, Type type, PersistentComponentState state)
    {
        var accessors = _cachedAccessorsByType.GetOrAdd(instance.GetType(), static (Type runtimeType, Type declaredType) => new PropertiesAccessor(runtimeType, declaredType), type);
        foreach (var (key, propertyType) in accessors.KeyTypePairs)
        {
            var (setter, getter) = accessors.GetAccessor(key);
            var value = getter.GetValue(instance);
            if (value != null)
            {
                state.PersistAsJson(key, value, propertyType);
            }
        }
    }

    private Type? ResolveType(string assembly, string fullTypeName) => _persistentServiceTypeCache.GetPersistentService(assembly, fullTypeName);

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    internal void Restore(PersistentComponentState state)
    {
        if (_registrations?.Any() != true &&
           state.TryTakeFromJson<IEnumerable<PersistentComponentRegistration>>(_registryKey, out var registry) &&
           registry != null)
        {
            _registrations = registry;
        }

        RestoreRegistrationsIfAvailable(state);
    }

    internal void RegisterForPersistence(PersistentComponentState state)
    {
        if (!_subscription.Equals(default(PersistingComponentStateSubscription)))
        {
            return;
        }

        _subscription = state.RegisterOnPersisting(() =>
        {
            PersistServicesState(state);
            return Task.CompletedTask;
        });
    }

    private sealed class PropertiesAccessor
    {
        internal const BindingFlags BindablePropertyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

        private readonly Dictionary<string, (PropertySetter, PropertyGetter)> _underlyingAccessors;
        private readonly (string, Type)[] _cachedKeysForService;

        public PropertiesAccessor([DynamicallyAccessedMembers(LinkerFlags.Component)] Type targetType, Type keyType)
        {
            _underlyingAccessors = new Dictionary<string, (PropertySetter, PropertyGetter)>(StringComparer.OrdinalIgnoreCase);

            var keys = new List<(string, Type)>();
            foreach (var propertyInfo in GetCandidateBindableProperties(targetType))
            {
                SupplyParameterFromPersistentComponentStateAttribute? parameterAttribute = null;
                foreach (var attribute in propertyInfo.GetCustomAttributes())
                {
                    if (attribute is SupplyParameterFromPersistentComponentStateAttribute persistentStateAttribute)
                    {
                        parameterAttribute = persistentStateAttribute;
                        break;
                    }
                }
                if (parameterAttribute == null)
                {
                    continue;
                }

                var propertyName = propertyInfo.Name;
                var key = ComputeKey(keyType, propertyName);
                keys.Add(new(key, propertyInfo.PropertyType));
                if (propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic)
                {
                    throw new InvalidOperationException(
                        $"The type '{targetType.FullName}' declares a property matching the name '{propertyName}' that is not public. Persistent service properties must be public.");
                }

                if (propertyInfo.GetMethod == null || !propertyInfo.GetMethod.IsPublic)
                {
                    throw new InvalidOperationException(
                        $"The type '{targetType.FullName}' declares a property matching the name '{propertyName}' that is not public. Persistent service properties must be public.");
                }

                var propertySetter = new PropertySetter(targetType, propertyInfo);
                var propertyGetter = new PropertyGetter(targetType, propertyInfo);

                _underlyingAccessors.Add(key, (propertySetter, propertyGetter));
            }

            _cachedKeysForService = [.. keys];
        }

        public (string, Type)[] KeyTypePairs => _cachedKeysForService;

        private static string ComputeKey(Type keyType, string propertyName)
        {
            // This happens once per type and property combo, so allocations are ok.
            var assemblyName = keyType.Assembly.FullName;
            var typeName = keyType.FullName;
            var input = Encoding.UTF8.GetBytes(string.Join(".", assemblyName, typeName, propertyName));
            return Convert.ToBase64String(SHA256.HashData(input));
        }

        internal static IEnumerable<PropertyInfo> GetCandidateBindableProperties(
            [DynamicallyAccessedMembers(LinkerFlags.Component)] Type targetType)
            => MemberAssignment.GetPropertiesIncludingInherited(targetType, BindablePropertyFlags);

        internal (PropertySetter setter, PropertyGetter getter) GetAccessor(string key)
        {
            return _underlyingAccessors.TryGetValue(key, out var result) ? result : default;
        }
    }
}
