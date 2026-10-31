// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop;

/// <summary>
/// Provides convenience methods to produce a <see cref="DotNetObjectReference{TValue}" />.
/// </summary>
public static class DotNetObjectReference
{
    /// <summary>
    /// Creates a new instance of <see cref="DotNetObjectReference{TValue}" />.
    /// </summary>
    /// <param name="value">The reference type to track.</param>
    /// <returns>An instance of <see cref="DotNetObjectReference{TValue}" />.</returns>
    public static DotNetObjectReference<TValue> Create<[DynamicallyAccessedMembers(JSInvokable)] TValue>(TValue value) where TValue : class
    {
        ArgumentNullException.ThrowIfNull(value);

        return new DotNetObjectReference<TValue>(value);
    }
}
