// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Configuration;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.SystemWeb
{
    /// <summary>
    /// A <see cref="DataProtector"/> that can be used by ASP.NET 4.x to interact with ASP.NET Core's
    /// DataProtection stack. This type is for internal use only and shouldn't be directly used by
    /// developers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class CompatibilityDataProtector : DataProtector
    {
        private static readonly Lazy<IDataProtectionProvider> _lazyProtectionProvider = new Lazy<IDataProtectionProvider>(CreateProtectionProvider);

        [ThreadStatic]
        private static bool _suppressPrimaryPurpose;

        private readonly Lazy<IDataProtector> _lazyProtector;
        private readonly Lazy<IDataProtector> _lazyProtectorSuppressedPrimaryPurpose;

        public CompatibilityDataProtector(string applicationName, string primaryPurpose, string[] specificPurposes)
            : base("application-name", "primary-purpose", null) // we feed dummy values to the base ctor
        {
            // We don't want to evaluate the IDataProtectionProvider factory quite yet,
            // as we'd rather defer failures to the call to Protect so that we can bubble
            // up a good error message to the developer.

            _lazyProtector = new Lazy<IDataProtector>(() => _lazyProtectionProvider.Value.CreateProtector(primaryPurpose, specificPurposes));

            // System.Web always provides "User.MachineKey.Protect" as the primary purpose for calls
            // to MachineKey.Protect. Only in this case should we allow suppressing the primary
            // purpose, as then we can easily map calls to MachineKey.Protect(userData, purposes)
            // into calls to provider.GetProtector(purposes).Protect(userData).
            if (primaryPurpose == "User.MachineKey.Protect")
            {
                _lazyProtectorSuppressedPrimaryPurpose = new Lazy<IDataProtector>(() => _lazyProtectionProvider.Value.CreateProtector(specificPurposes));
            }
            else
            {
                _lazyProtectorSuppressedPrimaryPurpose = _lazyProtector;
            }
        }

        // We take care of flowing purposes ourselves.
        protected override bool PrependHashedPurposeToPlaintext { get; } = false;

        // Retrieves the appropriate protector (potentially with a suppressed primary purpose) for this operation.
        private IDataProtector Protector => ((_suppressPrimaryPurpose) ? _lazyProtectorSuppressedPrimaryPurpose : _lazyProtector).Value;

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
                return Protector.Protect(userData);
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
            return Protector.Unprotect(encryptedData);
        }

        /// <summary>
        /// Invokes a delegate where calls to <see cref="ProviderProtect(byte[])"/>
        /// and <see cref="ProviderUnprotect(byte[])"/> will ignore the primary
        /// purpose and instead use only the sub-purposes.
        /// </summary>
        public static byte[] RunWithSuppressedPrimaryPurpose(Func<object, byte[], byte[]> callback, object state, byte[] input)
        {
            if (_suppressPrimaryPurpose)
            {
                return callback(state, input); // already suppressed - just forward call
            }

            try
            {
                try
                {
                    _suppressPrimaryPurpose = true;
                    return callback(state, input);
                }
                finally
                {
                    _suppressPrimaryPurpose = false;
                }
            }
            catch
            {
                // defeat exception filters
                throw;
            }
        }
    }
}
