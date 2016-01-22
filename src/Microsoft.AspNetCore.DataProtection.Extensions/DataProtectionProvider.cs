// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// A simple implementation of an <see cref="IDataProtectionProvider"/> where keys are stored
    /// at a particular location on the file system.
    /// </summary>
    public sealed class DataProtectionProvider : IDataProtectionProvider
    {
        private readonly IDataProtectionProvider _innerProvider;

        /// <summary>
        /// Creates an <see cref="DataProtectionProvider"/> given a location at which to store keys.
        /// </summary>
        /// <param name="keyDirectory">The <see cref="DirectoryInfo"/> in which keys should be stored. This may
        /// represent a directory on a local disk or a UNC share.</param>
        public DataProtectionProvider(DirectoryInfo keyDirectory)
            : this(keyDirectory, configure: null)
        {
        }

        /// <summary>
        /// Creates an <see cref="DataProtectionProvider"/> given a location at which to store keys and an
        /// optional configuration callback.
        /// </summary>
        /// <param name="keyDirectory">The <see cref="DirectoryInfo"/> in which keys should be stored. This may
        /// represent a directory on a local disk or a UNC share.</param>
        /// <param name="configure">An optional callback which provides further configuration of the data protection
        /// system. See <see cref="DataProtectionConfiguration"/> for more information.</param>
        public DataProtectionProvider(DirectoryInfo keyDirectory, Action<DataProtectionConfiguration> configure)
        {
            if (keyDirectory == null)
            {
                throw new ArgumentNullException(nameof(keyDirectory));
            }

            // build the service collection
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection(configurationObject =>
            {
                configurationObject.PersistKeysToFileSystem(keyDirectory);
                configure?.Invoke(configurationObject);
            });

            // extract the provider instance from the service collection
            _innerProvider = serviceCollection.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        }

        /// <summary>
        /// Implements <see cref="IDataProtectionProvider.CreateProtector(string)"/>.
        /// </summary>
        public IDataProtector CreateProtector(string purpose)
        {
            if (purpose == null)
            {
                throw new ArgumentNullException(nameof(purpose));
            }

            return _innerProvider.CreateProtector(purpose);
        }
    }
}
