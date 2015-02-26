// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.AspNet.DataProtection.Compatibility
{
    internal sealed class DataProtectorHelper
    {
        private IDataProtector _dataProtector;

        private DataProtectorHelper() { } // can only be instantaited by self

        public static IDataProtector GetDataProtector(ref DataProtectorHelper helperRef, IDataProtectionProvider protectionProvider, IFactorySupportFunctions supportFunctions)
        {
            // First, make sure that only one thread ever initializes the helper instance.
            var helper = Volatile.Read(ref helperRef);
            if (helper == null)
            {
                var newHelper = new DataProtectorHelper();
                helper = Interlocked.CompareExchange(ref helperRef, newHelper, null) ?? newHelper;
            }

            // Has the protector already been created?
            var protector = Volatile.Read(ref helper._dataProtector);
            if (protector == null)
            {
                // Since the helper is accessed by reference, all threads should agree on the one true helper
                // instance, so this lock is global given a particular reference. This is an implementation
                // of the double-check lock pattern.
                lock (helper)
                {
                    protector = Volatile.Read(ref helper._dataProtector);
                    if (protector == null)
                    {
                        protector = supportFunctions.CreateDataProtector(protectionProvider);
                        Volatile.Write(ref helper._dataProtector, protector);
                    }
                }
            }

            // And we're done!
            Debug.Assert(protector != null);
            return protector;
        }
    }
}
