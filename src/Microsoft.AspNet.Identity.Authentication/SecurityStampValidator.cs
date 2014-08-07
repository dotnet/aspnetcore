// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity.Authentication
{
    /// <summary>
    ///     Static helper class used to configure a CookieAuthenticationProvider to validate a cookie against a user's security
    ///     stamp
    /// </summary>
    public static class SecurityStampValidator
    {
        /// <summary>
        ///     Can be used as the ValidateIdentity method for a CookieAuthenticationProvider which will check a user's security
        ///     stamp after validateInterval
        ///     Rejects the identity if the stamp changes, and otherwise will call regenerateIdentity to sign in a new
        ///     ClaimsIdentity
        /// </summary>
        /// <typeparam name="TUser"></typeparam>
        /// <param name="validateInterval"></param>
        /// <param name="regenerateIdentity"></param>
        /// <returns></returns>
        public static Func<CookieValidateIdentityContext, Task> OnValidateIdentity<TUser>(
            TimeSpan validateInterval)
            where TUser : class
        {
            return OnValidateIdentity<TUser>(validateInterval, id => id.GetUserId());
        }

        /// <summary>
        ///     Can be used as the ValidateIdentity method for a CookieAuthenticationProvider which will check a user's security
        ///     stamp after validateInterval
        ///     Rejects the identity if the stamp changes, and otherwise will call regenerateIdentity to sign in a new
        ///     ClaimsIdentity
        /// </summary>
        /// <typeparam name="TUser"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="validateInterval"></param>
        /// <param name="regenerateIdentityCallback"></param>
        /// <param name="getUserIdCallback"></param>
        /// <returns></returns>
        public static Func<CookieValidateIdentityContext, Task> OnValidateIdentity<TUser>(
            TimeSpan validateInterval,
            Func<ClaimsIdentity, string> getUserIdCallback)
            where TUser : class
        {
            return async context =>
            {
                var currentUtc = DateTimeOffset.UtcNow;
                if (context.Options != null && context.Options.SystemClock != null)
                {
                    currentUtc = context.Options.SystemClock.UtcNow;
                }
                var issuedUtc = context.Properties.IssuedUtc;

                // Only validate if enough time has elapsed
                var validate = (issuedUtc == null);
                if (issuedUtc != null)
                {
                    var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                    validate = timeElapsed > validateInterval;
                }
                if (validate)
                {
                    var manager = context.HttpContext.RequestServices.GetService<SignInManager<TUser>>();
                    var userId = getUserIdCallback(context.Identity);
                    var user = await manager.ValidateSecurityStampAsync(context.Identity, userId);
                    if (user != null)
                    {
                        bool isPersistent = false;
                        if (context.Properties != null)
                        {
                            isPersistent = context.Properties.IsPersistent;
                        }
                        await manager.SignInAsync(user, isPersistent);
                    }
                    else
                    {
                        context.RejectIdentity();
                        manager.SignOut();
                    }
                }
            };
        }
    }
}