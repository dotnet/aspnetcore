// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components;

internal readonly struct CascadingParameterState
    (in CascadingParameterInfo parameterInfo, ICascadingValueSupplier valueSupplier, object? key)
{
    public CascadingParameterInfo ParameterInfo { get; } = parameterInfo;
    public ICascadingValueSupplier ValueSupplier { get; } = valueSupplier;
    public object? Key { get; } = key;

    public CascadingParameterState(in CascadingParameterInfo parameterInfo, ICascadingValueSupplier valueSupplier)
        : this(parameterInfo, valueSupplier, key: null) { }

    public static IReadOnlyList<CascadingParameterState> FindCascadingParameters(ComponentState componentState, out bool hasSingleDeliveryParameters)
    {
        var infos = GetCascadingParameterInfos(componentState.ComponentTypeInfo);

        hasSingleDeliveryParameters = false;

        // For components known not to have any cascading parameters, bail out early
        if (infos.Count == 0)
        {
            return Array.Empty<CascadingParameterState>();
        }

        // Now try to find matches for each of the cascading parameters
        // Defer instantiation of the result list until we know there's at least one
        List<CascadingParameterState>? resultStates = null;

        var numInfos = infos.Count;
        for (var infoIndex = 0; infoIndex < numInfos; infoIndex++)
        {
            var info = infos[infoIndex];
            var supplier = GetMatchingCascadingValueSupplier(info, componentState.Renderer, componentState.LogicalParentComponentState);
            if (supplier != null)
            {
                // Although not all parameters might be matched, we know the maximum number
                resultStates ??= new List<CascadingParameterState>(infos.Count - infoIndex);
                resultStates.Add(new CascadingParameterState(info, supplier, componentState));

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

    private static IReadOnlyList<CascadingParameterInfo> GetCascadingParameterInfos(
        ComponentTypeInfo typeInfo)
    {
        List<CascadingParameterInfo>? result = null;
        foreach (var parameter in typeInfo.Parameters)
        {
            if (parameter.Attribute is CascadingParameterAttributeBase cascadingParameterAttribute)
            {
                result ??= new List<CascadingParameterInfo>();
                result.Add(new CascadingParameterInfo(
                    cascadingParameterAttribute,
                    parameter.Name,
                    parameter.ParameterType));
            }
        }

        return result ?? [];
    }
}
