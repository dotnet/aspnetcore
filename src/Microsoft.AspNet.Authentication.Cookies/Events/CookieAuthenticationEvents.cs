// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// This default implementation of the ICookieAuthenticationEvents may be used if the 
    /// application only needs to override a few of the interface methods. This may be used as a base class
    /// or may be instantiated directly.
    /// </summary>
    public class CookieAuthenticationEvents : ICookieAuthenticationEvents
    {
        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Func<CookieValidatePrincipalContext, Task> OnValidatePrincipal { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Func<CookieResponseSignInContext, Task> OnResponseSignIn { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Func<CookieResponseSignedInContext, Task> OnResponseSignedIn { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Func<CookieResponseSignOutContext, Task> OnResponseSignOut { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Func<CookieApplyRedirectContext, Task> OnApplyRedirect { get; set; } = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.FromResult(0);
        };

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Func<CookieExceptionContext, Task> OnException { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual Task ValidatePrincipal(CookieValidatePrincipalContext context) => OnValidatePrincipal(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        public virtual Task ResponseSignIn(CookieResponseSignInContext context) => OnResponseSignIn(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        public virtual Task ResponseSignedIn(CookieResponseSignedInContext context) => OnResponseSignedIn(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        public virtual Task ResponseSignOut(CookieResponseSignOutContext context) => OnResponseSignOut(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        public virtual Task ApplyRedirect(CookieApplyRedirectContext context) => OnApplyRedirect(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        public virtual Task Exception(CookieExceptionContext context) => OnException(context);
    }
}