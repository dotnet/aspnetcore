// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNet.DataProtection.Dpapi
{
    // Provides a temporary implementation of IDataProtectionProvider for non-Windows machines
    // or for Windows machines where we can't depend on the user profile.
    internal sealed class DpapiDataProtectionProvider : IDataProtectionProvider
    {
        private readonly DpapiDataProtector _innerProtector;

        public DpapiDataProtectionProvider(DataProtectionScope scope)
        {
            _innerProtector = new DpapiDataProtector(new ProtectedDataImpl(), new byte[0], scope);
        }

        public IDataProtector CreateProtector([NotNull] string purpose)
        {
            return _innerProtector.CreateProtector(purpose);
        }
    }
}
