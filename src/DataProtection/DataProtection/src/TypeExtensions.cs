// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Helpful extension methods on <see cref="Type"/>.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Throws <see cref="InvalidCastException"/> if <paramref name="implementationType"/>
        /// is not assignable to <paramref name="expectedBaseType"/>.
        /// </summary>
        public static void AssertIsAssignableFrom(this Type expectedBaseType, Type implementationType)
        {
            if (!expectedBaseType.IsAssignableFrom(implementationType))
            {
                // It might seem a bit weird to throw an InvalidCastException explicitly rather than
                // to let the CLR generate one, but searching through NetFX there is indeed precedent
                // for this pattern when the caller knows ahead of time the operation will fail.
                throw new InvalidCastException(Resources.FormatTypeExtensions_BadCast(
                    expectedBaseType.AssemblyQualifiedName, implementationType.AssemblyQualifiedName));
            }
        }
    }
}
