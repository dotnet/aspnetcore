// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Configuration;
using System.Security.Cryptography;

namespace Microsoft.AspNet.DataProtection.SystemWeb
{
    /// <summary>
    /// A <see cref="DataProtector"/> that can be used by ASP.NET 4.x to interact with ASP.NET 5's
    /// DataProtection stack. This type is for internal use only and shouldn't be directly used by
    /// developers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class CompatibilityDataProtector : DataProtector
    {
        private static readonly Lazy<IDataProtectionProvider> _lazyProtectionProvider = new Lazy<IDataProtectionProvider>(CreateProtectionProvider);

        private readonly Lazy<IDataProtector> _lazyProtector;

        public CompatibilityDataProtector(string applicationName, string primaryPurpose, string[] specificPurposes)
            : base("application-name", "primary-purpose", null) // we feed dummy values to the base ctor
        {
            // We don't want to evaluate the IDataProtectionProvider factory quite yet,
            // as we'd rather defer failures to the call to Protect so that we can bubble
            // up a good error message to the developer.

            _lazyProtector = new Lazy<IDataProtector>(() => _lazyProtectionProvider.Value.CreateProtector(primaryPurpose, specificPurposes));
        }

        // We take care of flowing purposes ourselves.
        protected override bool PrependHashedPurposeToPlaintext { get; } = false;

        private static IDataProtectionProvider CreateProtectionProvider()
        {
            // Read from <appSettings> the startup type we need to use, then create it
            const string APPSETTINGS_KEY = "aspnet:dataProtectionStartupType";
            string startupTypeName = ConfigurationManager.AppSettings[APPSETTINGS_KEY];
            if (String.IsNullOrEmpty(startupTypeName))
            {
                // fall back to default startup type if one hasn't been specified in config
                startupTypeName = typeof(DataProtectionStartup).AssemblyQualifiedName;
            }
            Type startupType = Type.GetType(startupTypeName, throwOnError: true);
            var startupInstance = (DataProtectionStartup)Activator.CreateInstance(startupType);

            // Use it to initialize the system.
            return startupInstance.InternalConfigureServicesAndCreateProtectionProvider();
        }

        public override bool IsReprotectRequired(byte[] encryptedData)
        {
            // Nobody ever calls this.
            return false;
        }

        protected override byte[] ProviderProtect(byte[] userData)
        {
            try
            {
                return _lazyProtector.Value.Protect(userData);
            }
            catch (Exception ex)
            {
                // System.Web special-cases ConfigurationException errors and allows them to bubble
                // up to the developer without being homogenized. Since a call to Protect should
                // never fail, any exceptions here really do imply a misconfiguration.

#pragma warning disable CS0618 // Type or member is obsolete
                throw new ConfigurationException(Resources.DataProtector_ProtectFailed, ex);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        protected override byte[] ProviderUnprotect(byte[] encryptedData)
        {
            return _lazyProtector.Value.Unprotect(encryptedData);
        }
    }
}
