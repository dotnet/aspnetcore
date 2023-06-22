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
    private static readonly ConcurrentDictionary<Type, CascadingParameterInfo[]> _cachedInfos = new();

    public CascadingParameterInfo ParameterInfo { get; }
    public ICascadingValueSupplier ValueSupplier { get; }

    public CascadingParameterState(in CascadingParameterInfo parameterInfo, ICascadingValueSupplier valueSupplier)
    {
        ParameterInfo = parameterInfo;
        ValueSupplier = valueSupplier;
    }

    public static IReadOnlyList<CascadingParameterState> FindCascadingParameters(ComponentState componentState)
    {
        var componentType = componentState.Component.GetType();
        var infos = GetCascadingParameterInfos(componentType);

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
                resultStates.Add(new CascadingParameterState(info, supplier));
            }
        }

        return resultStates ?? (IReadOnlyList<CascadingParameterState>)Array.Empty<CascadingParameterState>();
    }

    private static ICascadingValueSupplier? GetMatchingCascadingValueSupplier(in CascadingParameterInfo info, ComponentState componentState)
    {
        var candidate = componentState;
        do
        {
            if (candidate.Component is ICascadingValueSupplier valueSupplier && valueSupplier.CanSupplyValue(info))
            {
                return valueSupplier;
            }

            candidate = candidate.LogicalParentComponentState;
        } while (candidate != null);

        // No match
        return null;
    }

    private static CascadingParameterInfo[] GetCascadingParameterInfos(
        [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (!_cachedInfos.TryGetValue(componentType, out var infos))
        {
            infos = CreateCascadingParameterInfos(componentType);
            _cachedInfos[componentType] = infos;
        }

        return infos;
    }

    private static CascadingParameterInfo[] CreateCascadingParameterInfos(
        [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        List<CascadingParameterInfo>? result = null;
        var candidateProps = ComponentProperties.GetCandidateBindableProperties(componentType);
        foreach (var prop in candidateProps)
        {
            var cascadingParameterAttribute = prop.GetCustomAttributes()
                .OfType<CascadingParameterAttributeBase>().SingleOrDefault();
            if (cascadingParameterAttribute != null)
            {
                result ??= new List<CascadingParameterInfo>();
                result.Add(new CascadingParameterInfo(
                    cascadingParameterAttribute,
                    prop.Name,
                    prop.PropertyType));
            }
        }

        return result?.ToArray() ?? Array.Empty<CascadingParameterInfo>();
    }
}
