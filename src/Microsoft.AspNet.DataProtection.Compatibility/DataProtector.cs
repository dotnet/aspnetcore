// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Microsoft.AspNet.DataProtection.Compatibility
{
    public sealed class DataProtector<T> : DataProtector, IFactorySupportFunctions
        where T : class, IDataProtectionProviderFactory, new()
    {
        private static DataProtectionProviderHelper _staticHelper;
        private DataProtectorHelper _helper;

        public DataProtector(string applicationName, string primaryPurpose, string[] specificPurposes)
            : base(applicationName, primaryPurpose, specificPurposes)
        {
        }

        protected override bool PrependHashedPurposeToPlaintext
        {
            get
            {
                return false;
            }
        }

        private IDataProtector GetCachedDataProtector()
        {
            var dataProtectionProvider = DataProtectionProviderHelper.GetDataProtectionProvider(ref _staticHelper, this);
            return DataProtectorHelper.GetDataProtector(ref _helper, dataProtectionProvider, this);
        }

        public override bool IsReprotectRequired(byte[] encryptedData)
        {
            return false;
        }

        protected override byte[] ProviderProtect(byte[] userData)
        {
            return GetCachedDataProtector().Protect(userData);
        }

        protected override byte[] ProviderUnprotect(byte[] encryptedData)
        {
            return GetCachedDataProtector().Unprotect(encryptedData);
        }

        IDataProtectionProvider IFactorySupportFunctions.CreateDataProtectionProvider()
        {
            IDataProtectionProviderFactory factory = Activator.CreateInstance<T>();
            IDataProtectionProvider dataProtectionProvider = factory.CreateDataProtectionProvider();
            Debug.Assert(dataProtectionProvider != null);
            return dataProtectionProvider;
        }

        IDataProtector IFactorySupportFunctions.CreateDataProtector(IDataProtectionProvider dataProtectionProvider)
        {
            Debug.Assert(dataProtectionProvider != null);

            IDataProtector dataProtector = dataProtectionProvider.CreateProtector(ApplicationName).CreateProtector(PrimaryPurpose);
            foreach (string specificPurpose in SpecificPurposes)
            {
                dataProtector = dataProtector.CreateProtector(specificPurpose);
            }

            Debug.Assert(dataProtector != null);
            return dataProtector;
        }
    }
}
