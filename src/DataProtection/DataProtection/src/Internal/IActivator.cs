// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.DataProtection.Internal;

/// <summary>
/// An interface into <see cref="Activator.CreateInstance{T}"/> that also supports
/// limited dependency injection (of <see cref="IServiceProvider"/>).
/// </summary>
public interface IActivator
{
    /// <summary>
    /// Creates an instance of <paramref name="implementationTypeName"/> and ensures
    /// that it is assignable to <paramref name="expectedBaseType"/>.
    /// </summary>
    object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type expectedBaseType, string implementationTypeName);
}
