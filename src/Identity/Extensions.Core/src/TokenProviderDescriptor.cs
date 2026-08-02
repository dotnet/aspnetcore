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
    // Provides support for multiple TUser types at once.
    // See MapIdentityApiTests.CanAddEndpointsToMultipleRouteGroupsForMultipleUsersTypes for example usage.
    private readonly Stack<Type> _providerTypes = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenProviderDescriptor"/> class.
    /// </summary>
    /// <param name="type">The concrete type for this token provider.</param>
    public TokenProviderDescriptor(Type type)
    {
        _providerTypes.Push(type);
    }

    /// <summary>
    /// The type that will be used for this token provider.
    /// </summary>
    public Type ProviderType => _providerTypes.Peek();

    /// <summary>
    /// If specified, the instance to be used for the token provider.
    /// </summary>
    public object? ProviderInstance { get; set; }

    internal void AddProviderType(Type type) => _providerTypes.Push(type);

    internal Type? GetProviderType<T>()
    {
        foreach (var providerType in _providerTypes)
        {
            if (typeof(T).IsAssignableFrom(providerType))
            {
                return providerType;
            }
        }
        return null;
    }
}
