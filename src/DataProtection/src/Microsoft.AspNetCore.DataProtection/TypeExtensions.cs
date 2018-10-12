// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
