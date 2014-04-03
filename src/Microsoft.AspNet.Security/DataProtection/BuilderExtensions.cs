// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Security.DataProtection
{
    public static class BuilderExtensions
    {
        public static IDataProtector CreateDataProtector(this IBuilder app, params string[] purposes)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            var dataProtectionProvider = (IDataProtectionProvider)app.ServiceProvider.GetService(typeof(IDataProtectionProvider));
            if (dataProtectionProvider == null)
            {
                dataProtectionProvider = DataProtectionProvider.CreateFromDpapi();
            }

            return dataProtectionProvider.CreateProtector(string.Join(";", purposes));
        }
    }
}
