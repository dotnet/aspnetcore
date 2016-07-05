// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Interop;

namespace Owin
{
    public static class CookieInterop
    {
        public static ISecureDataFormat<AuthenticationTicket> CreateSharedDataFormat(DirectoryInfo keyDirectory, string authenticationType)
        {
            var dataProtector = DataProtectionProvider.Create(keyDirectory)
                .CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", // full name of the ASP.NET 5 type
                authenticationType, "v2");
            return new AspNetTicketDataFormat(new DataProtectorShim(dataProtector));
        }
    }
}