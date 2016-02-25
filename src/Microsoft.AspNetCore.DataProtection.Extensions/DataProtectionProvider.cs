// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Contains factory methods for creating an <see cref="IDataProtectionProvider"/> where keys are stored
    /// at a particular location on the file system.
    /// </summary>
    /// <remarks>Use these methods when not using dependency injection to provide the service to the application.</remarks>
    public static class DataProtectionProvider
    {
        /// <summary>
        /// Creates an <see cref="DataProtectionProvider"/> given a location at which to store keys.
        /// </summary>
        /// <param name="keyDirectory">The <see cref="DirectoryInfo"/> in which keys should be stored. This may
        /// represent a directory on a local disk or a UNC share.</param>
        public static IDataProtectionProvider Create(DirectoryInfo keyDirectory)
        {
            return Create(keyDirectory, setupAction: builder => { });
        }

        /// <summary>
        /// Creates an <see cref="DataProtectionProvider"/> given a location at which to store keys and an
        /// optional configuration callback.
        /// </summary>
        /// <param name="keyDirectory">The <see cref="DirectoryInfo"/> in which keys should be stored. This may
        /// represent a directory on a local disk or a UNC share.</param>
        /// <param name="setupAction">An optional callback which provides further configuration of the data protection
        /// system. See <see cref="IDataProtectionBuilder"/> for more information.</param>
        public static IDataProtectionProvider Create(
            DirectoryInfo keyDirectory,
            Action<IDataProtectionBuilder> setupAction)
        {
            if (keyDirectory == null)
            {
                throw new ArgumentNullException(nameof(keyDirectory));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            // build the service collection
            var serviceCollection = new ServiceCollection();
            var builder = serviceCollection.AddDataProtection();
            builder.PersistKeysToFileSystem(keyDirectory);

            if (setupAction != null)
            {
                setupAction(builder);
            }

            // extract the provider instance from the service collection
            return serviceCollection.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        }
    }
}