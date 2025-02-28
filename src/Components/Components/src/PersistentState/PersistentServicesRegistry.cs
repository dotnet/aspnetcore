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

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal class PersistentServicesRegistry
{
    private static readonly string _registryKey = typeof(PersistentServicesRegistry).FullName!;

    private readonly IServiceProvider _serviceProvider;
    private readonly PersistentServiceTypeCache _persistentServiceTypeCache;
    private IPersistentComponentRegistration[] _registrations;
    private List<PersistingComponentStateSubscription> _subscriptions = [];
    private static readonly ConcurrentDictionary<Type, PropertiesAccessor> _cachedAccessorsByType = new();

    public IComponentRenderMode? RenderMode { get; internal set; }

    public PersistentServicesRegistry(IServiceProvider serviceProvider)
    {
        var registrations = serviceProvider.GetRequiredService<IEnumerable<IPersistentComponentRegistration>>();
        _serviceProvider = serviceProvider;
        _persistentServiceTypeCache = new PersistentServiceTypeCache();
        _registrations = [.. registrations.Distinct().Order()];
    }

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    private class PersistentComponentRegistration : IPersistentComponentRegistration
    {
        public string Assembly { get; set; } = "";

        public string FullTypeName { get; set; } = "";

        public IComponentRenderMode? GetRenderModeOrDefault() => null;

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
        var accessors = _cachedAccessorsByType.GetOrAdd(instance.GetType(), static (runtimeType, declaredType) => new PropertiesAccessor(runtimeType, declaredType), type);
        foreach (var (key, propertyType) in accessors.KeyTypePairs)
        {
            if (state.TryTakeFromJson(key, propertyType, out var result))
            {
                var (setter, getter) = accessors.GetAccessor(key);
                setter.SetValue(instance, result!);
            }
        }
    }

    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Components.PersistentComponentState.PersistAsJson(String, Object, Type)")]
    private static void PersistInstanceState(object instance, Type type, PersistentComponentState state)
    {
        var accessors = _cachedAccessorsByType.GetOrAdd(instance.GetType(), static (runtimeType, declaredType) => new PropertiesAccessor(runtimeType, declaredType), type);
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
        if (_registrations.Length == 0 &&
           state.TryTakeFromJson<PersistentComponentRegistration[]>(_registryKey, out var registry) &&
           registry != null)
        {
            _registrations = registry ?? [];
        }

        RestoreRegistrationsIfAvailable(state);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    internal void RegisterForPersistence(PersistentComponentState state)
    {
        if (_subscriptions.Count != 0)
        {
            return;
        }

        var subscriptions = new List<PersistingComponentStateSubscription>(_registrations.Length + 1);
        for (var i = 0; i < _registrations.Length; i++)
        {
            var registration = _registrations[i];
            var type = ResolveType(registration.Assembly, registration.FullTypeName);
            if (type == null)
            {
                continue;
            }

            var renderMode = registration.GetRenderModeOrDefault();

            var instance = _serviceProvider.GetRequiredService(type);
            subscriptions.Add(state.RegisterOnPersisting(() =>
            {
                PersistInstanceState(instance, type, state);
                return Task.CompletedTask;
            }, renderMode));
        }

        if(RenderMode != null)
        {
            subscriptions.Add(state.RegisterOnPersisting(() =>
            {
                state.PersistAsJson(_registryKey, _registrations);
                return Task.CompletedTask;
            }, RenderMode));
        }

        _subscriptions = subscriptions;
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

    private class RenderModeComparer : IEqualityComparer<IComponentRenderMode?>
    {
        public static RenderModeComparer Instance { get; } = new RenderModeComparer();

        public bool Equals(IComponentRenderMode? x, IComponentRenderMode? y)
        {
            return x?.GetType() == y?.GetType();
        }

        public int GetHashCode([DisallowNull] IComponentRenderMode? obj) => obj?.GetHashCode() ?? 1;
    }
}
