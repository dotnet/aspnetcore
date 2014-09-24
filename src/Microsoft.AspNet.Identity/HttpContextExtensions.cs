// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Http.Security;
using System.Linq;
using System;
using System.Security.Principal;

namespace Microsoft.AspNet.Http
{
    public static class HttpContextExtensions
    {
        private const string LoginProviderKey = "LoginProvider";
        private const string XsrfKey = "XsrfId";

        public static IEnumerable<AuthenticationDescription> GetExternalAuthenticationTypes(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.GetAuthenticationTypes().Where(d => !string.IsNullOrEmpty(d.Caption));
        }

        public static async Task<ExternalLoginInfo> GetExternalLoginInfo(this HttpContext context, string expectedXsrf = null)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            // REVIEW: should we consider taking the external authentication type as an argument?
            var auth = await context.AuthenticateAsync(ClaimsIdentityOptions.DefaultExternalLoginAuthenticationType);
            if (auth == null || auth.Identity == null || auth.Properties.Dictionary == null || !auth.Properties.Dictionary.ContainsKey(LoginProviderKey))
            {
                return null;
            }

            if (expectedXsrf != null)
            {
                if (!auth.Properties.Dictionary.ContainsKey(XsrfKey))
                {
                    return null;
                }
                var userId = auth.Properties.Dictionary[XsrfKey] as string;
                if (userId != expectedXsrf)
                {
                    return null;
                }
            }

            var providerKey = auth.Identity.FindFirstValue(ClaimTypes.NameIdentifier);
            var provider = auth.Properties.Dictionary[LoginProviderKey] as string;
            if (providerKey == null || provider == null)
            {
                return null;
            }
            return new ExternalLoginInfo(auth.Identity, provider, providerKey, auth.Description.Caption);
        }

        public static AuthenticationProperties ConfigureExternalAuthenticationProperties(this HttpContext context, string provider, string redirectUrl, string userId = null)
        {
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Dictionary[LoginProviderKey] = provider;
            if (userId != null)
            {
                properties.Dictionary[XsrfKey] = userId;
            }
            return properties;
        }
    }
}