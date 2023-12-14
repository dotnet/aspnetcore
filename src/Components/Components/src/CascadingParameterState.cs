// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
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

    public static IReadOnlyList<CascadingParameterState> FindCascadingParameters(ComponentState componentState, out bool hasSingleDeliveryParameters)
    {
        var componentType = componentState.Component.GetType();

        // Suppressed with "pragma warning disable" so ILLink Roslyn Anayzer doesn't report the warning.
        #pragma warning disable IL2072 // 'componentType' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'Microsoft.AspNetCore.Components.CascadingParameterState.GetCascadingParameterInfos(Type)'.
        var infos = GetCascadingParameterInfos(componentType);
        #pragma warning restore IL2072 // 'componentType' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'Microsoft.AspNetCore.Components.CascadingParameterState.GetCascadingParameterInfos(Type)'.

        hasSingleDeliveryParameters = false;

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
            var supplier = GetMatchingCascadingValueSupplier(info, componentState.Renderer, componentState.LogicalParentComponentState);
            if (supplier != null)
            {
                // Although not all parameters might be matched, we know the maximum number
                resultStates ??= new List<CascadingParameterState>(infos.Length - infoIndex);
                resultStates.Add(new CascadingParameterState(info, supplier));

                if (info.Attribute.SingleDelivery)
                {
                    hasSingleDeliveryParameters = true;
                    if (!supplier.IsFixed)
                    {
                        // We don't have a use case for IsFixed=false with SingleDelivery=true. To avoid complications about
                        // subscribing/unsubscribing in this case, just disallow it. It shouldn't be possible for this to
                        // occur unless someone creates their own CascadingParameterAttributeBase subclass.
                        throw new InvalidOperationException($"'{info.Attribute.GetType()}' is flagged with SingleDelivery, but the selected supplier '{supplier.GetType()}' is not flagged with {nameof(ICascadingValueSupplier.IsFixed)}");
                    }
                }
            }
        }

        return resultStates ?? (IReadOnlyList<CascadingParameterState>)Array.Empty<CascadingParameterState>();
    }

    internal static ICascadingValueSupplier? GetMatchingCascadingValueSupplier(in CascadingParameterInfo info, Renderer renderer, ComponentState? componentState)
    {
        // First scan up through the component hierarchy
        var candidate = componentState;
        while (candidate is not null)
        {
            if (candidate.Component is ICascadingValueSupplier valueSupplier && valueSupplier.CanSupplyValue(info))
            {
                return valueSupplier;
            }

            candidate = candidate.LogicalParentComponentState;
        }

        // We got to the root and found no match, so now look at the providers registered in DI
        foreach (var valueSupplier in renderer.ServiceProviderCascadingValueSuppliers)
        {
            if (valueSupplier.CanSupplyValue(info))
            {
                return valueSupplier;
            }
        }

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
