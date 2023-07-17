// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to represents a token provider in <see cref="TokenOptions"/>'s TokenMap.
/// </summary>
public class TokenProviderDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenProviderDescriptor"/> class.
    /// </summary>
    /// <param name="type">The concrete type for this token provider.</param>
    public TokenProviderDescriptor(Type type)
    {
        ProviderType = type;
    }

    /// <summary>
    /// The type that will be used for this token provider.
    /// </summary>
    public Type ProviderType { get; internal set; }

    /// <summary>
    /// If specified, the instance to be used for the token provider.
    /// </summary>
    public object? ProviderInstance { get; set; }

    // Temporary fix to test MapIdentityApi's support for multiple TUser and TContext.
    // There's nothing as permanent as a temporary fix, but it seems better than now support.
    internal List<Type>? OtherProviderTypes { get; set; }
}
