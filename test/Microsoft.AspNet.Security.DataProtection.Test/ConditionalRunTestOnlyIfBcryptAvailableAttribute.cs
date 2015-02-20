// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Cryptography.SafeHandles;
using Microsoft.AspNet.Testing.xunit;

namespace Microsoft.AspNet.Security.DataProtection.Test
{
    public class ConditionalRunTestOnlyIfBcryptAvailableAttribute : Attribute, ITestCondition
    {
        private static readonly SafeLibraryHandle _bcryptLibHandle = GetBcryptLibHandle();

        private readonly string _requiredExportFunction;

        public ConditionalRunTestOnlyIfBcryptAvailableAttribute(string requiredExportFunction = null)
        {
            _requiredExportFunction = requiredExportFunction;
        }

        public bool IsMet
        {
            get
            {
                if (_bcryptLibHandle == null)
                {
                    return false; // no bcrypt.dll available
                }

                return (_requiredExportFunction == null || _bcryptLibHandle.DoesProcExist(_requiredExportFunction));
            }
        }

        public string SkipReason
        {
            get
            {
                return (_bcryptLibHandle != null)
                    ? String.Format(CultureInfo.InvariantCulture, "Export {0} not found in bcrypt.dll", _requiredExportFunction)
                    : "bcrypt.dll not found on this platform.";
            }
        }

        private static SafeLibraryHandle GetBcryptLibHandle()
        {
            try
            {
                return SafeLibraryHandle.Open("bcrypt.dll");
            }
            catch
            {
                // If we're not on an OS with BCRYPT.DLL, just bail.
                return null;
            }
        }
    }
}
