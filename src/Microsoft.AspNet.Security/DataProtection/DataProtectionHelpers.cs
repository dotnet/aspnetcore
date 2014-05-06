// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Security.DataProtection
{
    public static class DataProtectionHelpers
    {
        public static IDataProtector CreateDataProtector(IDataProtectionProvider dataProtectionProvider, params string[] purposes)
        {
            if (dataProtectionProvider == null)
            {
                dataProtectionProvider = DataProtectionProvider.CreateFromDpapi();
            }

            return dataProtectionProvider.CreateProtector(string.Join(";", purposes));
        }
    }
}
