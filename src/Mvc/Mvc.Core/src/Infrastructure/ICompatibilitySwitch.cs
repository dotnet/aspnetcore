// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Defines a compatibility switch. This is framework infrastructure and should not be used
/// by application code.
/// </summary>
public interface ICompatibilitySwitch
{
    /// <summary>
    /// Gets a value indicating whether the <see cref="Value"/> property has been set.
    /// </summary>
    /// <remarks>
    /// This is used by the compatibility infrastructure to determine whether the application developer
    /// has set explicitly set the value associated with this switch.
    /// </remarks>
    bool IsValueSet { get; }

    /// <summary>
    /// Gets the name of the compatibility switch.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or set the value associated with the compatibility switch.
    /// </summary>
    /// <remarks>
    /// Setting the switch value using <see cref="Value"/> will not set <see cref="IsValueSet"/> to <c>true</c>.
    /// This should be used by the compatibility infrastructure when <see cref="IsValueSet"/> is <c>false</c>
    /// to apply a compatibility value based on <see cref="CompatibilityVersion"/>.
    /// </remarks>
    object Value { get; set; }
}
