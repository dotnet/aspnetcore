// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
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
        /// Creates a <see cref="DataProtectionProvider"/> that store keys in a location based on
        /// the platform and operating system.
        /// </summary>
        /// <param name="applicationName">An identifier that uniquely discriminates this application from all other
        /// applications on the machine.</param>
        public static IDataProtectionProvider Create(string applicationName)
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName));
            }

            return CreateProvider(
                keyDirectory: null,
                setupAction: builder => { builder.SetApplicationName(applicationName); },
                certificate: null);
        }

        /// <summary>
        /// Creates an <see cref="DataProtectionProvider"/> given a location at which to store keys.
        /// </summary>
        /// <param name="keyDirectory">The <see cref="DirectoryInfo"/> in which keys should be stored. This may
        /// represent a directory on a local disk or a UNC share.</param>
        public static IDataProtectionProvider Create(DirectoryInfo keyDirectory)
        {
            if (keyDirectory == null)
            {
                throw new ArgumentNullException(nameof(keyDirectory));
            }

            return CreateProvider(keyDirectory, setupAction: builder => { }, certificate: null);
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

            return CreateProvider(keyDirectory, setupAction, certificate: null);
        }

        /// <summary>
        /// Creates a <see cref="DataProtectionProvider"/> that store keys in a location based on
        /// the platform and operating system and uses the given <see cref="X509Certificate2"/> to encrypt the keys.
        /// </summary>
        /// <param name="applicationName">An identifier that uniquely discriminates this application from all other
        /// applications on the machine.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to be used for encryption.</param>
        public static IDataProtectionProvider Create(string applicationName, X509Certificate2 certificate)
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName));
            }
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            return CreateProvider(
                keyDirectory: null,
                setupAction: builder => { builder.SetApplicationName(applicationName); },
                certificate: certificate);
        }

        /// <summary>
        /// Creates an <see cref="DataProtectionProvider"/> given a location at which to store keys
        /// and a <see cref="X509Certificate2"/> used to encrypt the keys.
        /// </summary>
        /// <param name="keyDirectory">The <see cref="DirectoryInfo"/> in which keys should be stored. This may
        /// represent a directory on a local disk or a UNC share.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to be used for encryption.</param>
        public static IDataProtectionProvider Create(
            DirectoryInfo keyDirectory,
            X509Certificate2 certificate)
        {
            if (keyDirectory == null)
            {
                throw new ArgumentNullException(nameof(keyDirectory));
            }
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            return CreateProvider(keyDirectory, setupAction: builder => { }, certificate: certificate);
        }

        /// <summary>
        /// Creates an <see cref="DataProtectionProvider"/> given a location at which to store keys, an
        /// optional configuration callback and a <see cref="X509Certificate2"/> used to encrypt the keys.
        /// </summary>
        /// <param name="keyDirectory">The <see cref="DirectoryInfo"/> in which keys should be stored. This may
        /// represent a directory on a local disk or a UNC share.</param>
        /// <param name="setupAction">An optional callback which provides further configuration of the data protection
        /// system. See <see cref="IDataProtectionBuilder"/> for more information.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to be used for encryption.</param>
        public static IDataProtectionProvider Create(
            DirectoryInfo keyDirectory,
            Action<IDataProtectionBuilder> setupAction,
            X509Certificate2 certificate)
        {
            if (keyDirectory == null)
            {
                throw new ArgumentNullException(nameof(keyDirectory));
            }
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            return CreateProvider(keyDirectory, setupAction, certificate);
        }

        internal static IDataProtectionProvider CreateProvider(
            DirectoryInfo keyDirectory,
            Action<IDataProtectionBuilder> setupAction,
            X509Certificate2 certificate)
        {
            // build the service collection
            var serviceCollection = new ServiceCollection();
            var builder = serviceCollection.AddDataProtection();

            if (keyDirectory != null)
            {
                builder.PersistKeysToFileSystem(keyDirectory);
            }

            if (certificate != null)
            {
                builder.ProtectKeysWithCertificate(certificate);
            }

            setupAction(builder);

            // extract the provider instance from the service collection
            return serviceCollection.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        }
    }
}
