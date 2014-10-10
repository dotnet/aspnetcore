// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


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
                // TODO: Get this from the environment.
                dataProtectionProvider = new EphemeralDataProtectionProvider();
            }

            return dataProtectionProvider.CreateProtector(string.Join(";", purposes));
        }
    }
}
