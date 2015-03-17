// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection
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
        public DataProtectionProvider([NotNull] DirectoryInfo keyDirectory)
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
        public DataProtectionProvider([NotNull] DirectoryInfo keyDirectory, Action<DataProtectionConfiguration> configure)
        {
            // build the service collection
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            serviceCollection.ConfigureDataProtection(configurationObject =>
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
        public IDataProtector CreateProtector([NotNull] string purpose)
        {
            return _innerProvider.CreateProtector(purpose);
        }
    }
}
