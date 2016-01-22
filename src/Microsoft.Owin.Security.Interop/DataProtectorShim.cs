// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.Owin.Security.Interop
{
    /// <summary>
    /// Converts an <see cref="IDataProtector"/> to an
    /// <see cref="Microsoft.Owin.Security.DataProtection.IDataProtector"/>.
    /// </summary>
    public sealed class DataProtectorShim : Microsoft.Owin.Security.DataProtection.IDataProtector
    {
        private readonly IDataProtector _protector;

        public DataProtectorShim(IDataProtector protector)
        {
            _protector = protector;
        }

        public byte[] Protect(byte[] userData)
        {
            return _protector.Protect(userData);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return _protector.Unprotect(protectedData);
        }
    }
}