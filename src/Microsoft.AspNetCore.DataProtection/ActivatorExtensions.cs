// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Extension methods for working with <see cref="IActivator"/>.
    /// </summary>
    internal static class ActivatorExtensions
    {
        /// <summary>
        /// Creates an instance of <paramref name="implementationTypeName"/> and ensures
        /// that it is assignable to <typeparamref name="T"/>.
        /// </summary>
        public static T CreateInstance<T>(this IActivator activator, string implementationTypeName)
            where T : class
        {
            if (implementationTypeName == null)
            {
                throw new ArgumentNullException(nameof(implementationTypeName));
            }

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

        /// <summary>
        /// A simplified default implementation of <see cref="IActivator"/> that understands
        /// how to call ctors which take <see cref="IServiceProvider"/>.
        /// </summary>
        private sealed class SimpleActivator : IActivator
        {
            /// <summary>
            /// A default <see cref="SimpleActivator"/> whose wrapped <see cref="IServiceProvider"/> is null.
            /// </summary>
            internal static readonly SimpleActivator DefaultWithoutServices = new SimpleActivator(null);

            private readonly IServiceProvider _services;

            public SimpleActivator(IServiceProvider services)
            {
                _services = services;
            }

            public object CreateInstance(Type expectedBaseType, string implementationTypeName)
            {
                // Would the assignment even work?
                var implementationType = Type.GetType(implementationTypeName, throwOnError: true);
                expectedBaseType.AssertIsAssignableFrom(implementationType);

                // If no IServiceProvider was specified, prefer .ctor() [if it exists]
                if (_services == null)
                {
                    var ctorParameterless = implementationType.GetConstructor(Type.EmptyTypes);
                    if (ctorParameterless != null)
                    {
                        return Activator.CreateInstance(implementationType);
                    }
                }

                // If an IServiceProvider was specified or if .ctor() doesn't exist, prefer .ctor(IServiceProvider) [if it exists]
                var ctorWhichTakesServiceProvider = implementationType.GetConstructor(new Type[] { typeof(IServiceProvider) });
                if (ctorWhichTakesServiceProvider != null)
                {
                    return ctorWhichTakesServiceProvider.Invoke(new[] { _services });
                }

                // Finally, prefer .ctor() as an ultimate fallback.
                // This will throw if the ctor cannot be called.
                return Activator.CreateInstance(implementationType);
            }
        }
    }
}
