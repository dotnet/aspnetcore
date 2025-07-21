// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal struct ComponentSubscriptionKey(ComponentState subscriber, string propertyName) : IEquatable<ComponentSubscriptionKey>
{
    public ComponentState Subscriber { get; } = subscriber;

    public string PropertyName { get; } = propertyName;

    public bool Equals(ComponentSubscriptionKey other)
        => Subscriber == other.Subscriber && PropertyName == other.PropertyName;

    public override bool Equals(object? obj)
        => obj is ComponentSubscriptionKey other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Subscriber, PropertyName);
}
