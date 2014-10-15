// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    public class SecurityStampValidator<TUser> : ISecurityStampValidator where TUser : class
    {
        /// <summary>
        ///     Rejects the identity if the stamp changes, and otherwise will sign in a new
        ///     ClaimsIdentity
        /// </summary>
        /// <returns></returns>
        public virtual async Task Validate(CookieValidateIdentityContext context, ClaimsIdentity identity)
        {
            var manager = context.HttpContext.RequestServices.GetService<SignInManager<TUser>>();
            var userId = identity.GetUserId();
            var user = await manager.ValidateSecurityStampAsync(identity, userId);
            if (user != null)
            {
                var isPersistent = false;
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
    }

    /// <summary>
    ///     Static helper class used to configure a CookieAuthenticationNotifications to validate a cookie against a user's security
    ///     stamp
    /// </summary>
    public static class SecurityStampValidator
    {
        public static Task ValidateIdentityAsync(CookieValidateIdentityContext context)
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
                var identityOptions = context.HttpContext.RequestServices.GetService<IOptions<IdentityOptions>>().Options;
                validate = timeElapsed > identityOptions.SecurityStampValidationInterval;
            }
            if (validate)
            {
                var validator = context.HttpContext.RequestServices.GetService<ISecurityStampValidator>();
                return validator.Validate(context, context.Identity);
            }
            return Task.FromResult(0);
        }
    }
}