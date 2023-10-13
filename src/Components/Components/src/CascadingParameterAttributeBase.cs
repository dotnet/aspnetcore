// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a parameter whose value cascades down the component hierarchy.
/// </summary>
public abstract class CascadingParameterAttributeBase : Attribute
{
    /// <summary>
    /// Gets a flag indicating whether the cascading parameter should
    /// be supplied only once per component.
    /// </summary>
    internal virtual bool SingleDelivery => false;
}
