// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

internal enum CascadingParameterSubscriptionMode
{
    Initial,

    // Rebinds a retained component after its parameter metadata changed. Suppliers must not
    // replay values that are valid only during initial parameter supply.
    MetadataRefresh,
}

// Keep supplier-specific behavior behind this abstraction. Callers should extend the contract
// with semantic context instead of depending on concrete ICascadingValueSupplier implementations.
internal interface ICascadingValueSupplier
{
    bool IsFixed { get; }

    bool CanSupplyValue(in CascadingParameterInfo parameterInfo);

    object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo);

    void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo);

    void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo, CascadingParameterSubscriptionMode mode)
        => Subscribe(subscriber, parameterInfo);

    void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo);
}
