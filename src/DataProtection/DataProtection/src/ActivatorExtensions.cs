// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Extension methods for working with <see cref="IActivator"/>.
/// </summary>
internal static class ActivatorExtensions
{
    /// <summary>
    /// Creates an instance of <paramref name="implementationTypeName"/> and ensures
    /// that it is assignable to <typeparamref name="T"/>.
    /// </summary>
    public static T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IActivator activator, string implementationTypeName)
        where T : class
    {
        ArgumentNullThrowHelper.ThrowIfNull(implementationTypeName);

        return activator.CreateInstance(typeof(T), implementationTypeName) as T
            ?? CryptoUtil.Fail<T>("CreateInstance returned null.");
    }

    /// <summary>
    /// Returns a <see cref="IActivator"/> given an <see cref="IServiceProvider"/>.
    /// Guaranteed to return non-null, even if <paramref name="serviceProvider"/> is null.
    /// </summary>
    public static IActivator GetActivator(this IServiceProvider serviceProvider)
    {
        return (serviceProvider != null)
            ? (serviceProvider.GetService<IActivator>() ?? new SimpleActivator(serviceProvider))
            : SimpleActivator.DefaultWithoutServices;
    }
}
