// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;

namespace Identity.DefaultUI.WebSite
{
    public class ContosoAuthenticationOptions : AuthenticationSchemeOptions
    {
        public ContosoAuthenticationOptions()
        {
            Events = new object();
        }

        public string SignInScheme { get; set; }
        public string ReturnUrlQueryParameter { get; set; } = "returnUrl";
        public string RemoteLoginPath { get; set; } = "/Contoso/Login";
    }
}