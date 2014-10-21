// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.DataProtection
{
    public static class DataProtectionHelpers
    {
        public static IDataProtector CreateDataProtector([NotNull] this IDataProtectionProvider dataProtectionProvider, params string[] purposes)
        {
            return dataProtectionProvider.CreateProtector(string.Join(";", purposes));
        }
    }
}
