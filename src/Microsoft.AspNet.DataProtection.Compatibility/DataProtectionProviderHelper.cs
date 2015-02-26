// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.AspNet.DataProtection.Compatibility
{
    internal sealed class DataProtectionProviderHelper
    {
        private IDataProtectionProvider _dataProtectionProvider;

        private DataProtectionProviderHelper() { } // can only be instantaited by self

        public static IDataProtectionProvider GetDataProtectionProvider(ref DataProtectionProviderHelper helperRef, IFactorySupportFunctions supportFunctions)
        {
            // First, make sure that only one thread ever initializes the helper instance.
            var helper = Volatile.Read(ref helperRef);
            if (helper == null)
            {
                var newHelper = new DataProtectionProviderHelper();
                helper = Interlocked.CompareExchange(ref helperRef, newHelper, null) ?? newHelper;
            }

            // Has the provider already been created?
            var provider = Volatile.Read(ref helper._dataProtectionProvider);
            if (provider == null)
            {
                // Since the helper is accessed by reference, all threads should agree on the one true helper
                // instance, so this lock is global given a particular reference. This is an implementation
                // of the double-check lock pattern.
                lock (helper)
                {
                    provider = Volatile.Read(ref helper._dataProtectionProvider);
                    if (provider == null)
                    {
                        provider = supportFunctions.CreateDataProtectionProvider();
                        Volatile.Write(ref helper._dataProtectionProvider, provider);
                    }
                }
            }

            // And we're done!
            Debug.Assert(provider != null);
            return provider;
        }
    }
}
