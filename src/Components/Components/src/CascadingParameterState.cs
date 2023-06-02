// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal readonly struct CascadingParameterState
{
    private static readonly ConcurrentDictionary<Type, ReflectedCascadingParameterInfo[]> _cachedInfos = new();

    public string LocalValueName { get; }
    public ICascadingValueSupplier ValueSupplier { get; }

    public CascadingParameterState(string localValueName, ICascadingValueSupplier valueSupplier)
    {
        LocalValueName = localValueName;
        ValueSupplier = valueSupplier;
    }

    public static IReadOnlyList<CascadingParameterState> FindCascadingParameters(ComponentState componentState)
    {
        var componentType = componentState.Component.GetType();
        var infos = GetReflectedCascadingParameterInfos(componentType);

        // For components known not to have any cascading parameters, bail out early
        if (infos.Length == 0)
        {
            return Array.Empty<CascadingParameterState>();
        }

        // Now try to find matches for each of the cascading parameters
        // Defer instantiation of the result list until we know there's at least one
        List<CascadingParameterState>? resultStates = null;

        var numInfos = infos.Length;
        for (var infoIndex = 0; infoIndex < numInfos; infoIndex++)
        {
            ref var info = ref infos[infoIndex];
            var supplier = GetMatchingCascadingValueSupplier(info, componentState);
            if (supplier != null)
            {
                // Although not all parameters might be matched, we know the maximum number
                resultStates ??= new List<CascadingParameterState>(infos.Length - infoIndex);
                resultStates.Add(new CascadingParameterState(info.PropertyName, supplier));
            }
        }

        return resultStates ?? (IReadOnlyList<CascadingParameterState>)Array.Empty<CascadingParameterState>();
    }

    private static ICascadingValueSupplier? GetMatchingCascadingValueSupplier(in ReflectedCascadingParameterInfo info, ComponentState componentState)
    {
        var candidate = componentState;
        do
        {
            var candidateComponent = candidate.Component;
            if (candidateComponent is ICascadingValueSupplierFactory valueSupplierFactory && 
                valueSupplierFactory.TryGetValueSupplier(info.Attribute, info.PropertyType, info.PropertyName, out var valueSupplier))
            {
                return valueSupplier;
            }

            candidate = candidate.ParentComponentState;
        } while (candidate != null);

        // No match
        return null;
    }

    private static ReflectedCascadingParameterInfo[] GetReflectedCascadingParameterInfos(
        [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (!_cachedInfos.TryGetValue(componentType, out var infos))
        {
            infos = CreateReflectedCascadingParameterInfos(componentType);
            _cachedInfos[componentType] = infos;
        }

        return infos;
    }

    private static ReflectedCascadingParameterInfo[] CreateReflectedCascadingParameterInfos(
        [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        List<ReflectedCascadingParameterInfo>? result = null;
        var candidateProps = ComponentProperties.GetCandidateBindableProperties(componentType);
        foreach (var prop in candidateProps)
        {
            var cascadingParameterAttribute = prop.GetCustomAttributes()
                .OfType<ICascadingParameterAttribute>().SingleOrDefault();
            if (cascadingParameterAttribute != null)
            {
                result ??= new List<ReflectedCascadingParameterInfo>();
                result.Add(new ReflectedCascadingParameterInfo(
                    cascadingParameterAttribute,
                    prop.Name,
                    prop.PropertyType));
            }
        }

        return result?.ToArray() ?? Array.Empty<ReflectedCascadingParameterInfo>();
    }

    readonly struct ReflectedCascadingParameterInfo
    {
        public object Attribute { get; }
        public string PropertyName { get; }
        public Type PropertyType { get; }

        public ReflectedCascadingParameterInfo(
            object attribute,
            string propertyName,
            Type propertyType)
        {
            Attribute = attribute;
            PropertyName = propertyName;
            PropertyType = propertyType;
        }
    }
}
