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
        public Action<CookieResponseSignInContext> OnResponseSignIn { get; set; } = context => { };

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Action<CookieResponseSignedInContext> OnResponseSignedIn { get; set; } = context => { };

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Action<CookieResponseSignOutContext> OnResponseSignOut { get; set; } = context => { };

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Action<CookieApplyRedirectContext> OnApplyRedirect { get; set; } = context => context.Response.Redirect(context.RedirectUri);

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Action<CookieExceptionContext> OnException { get; set; } = context => { };

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
        public virtual void ResponseSignIn(CookieResponseSignInContext context) => OnResponseSignIn(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        public virtual void ResponseSignedIn(CookieResponseSignedInContext context) => OnResponseSignedIn(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        public virtual void ResponseSignOut(CookieResponseSignOutContext context) => OnResponseSignOut(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        public virtual void ApplyRedirect(CookieApplyRedirectContext context) => OnApplyRedirect(context);

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        public virtual void Exception(CookieExceptionContext context) => OnException(context);
    }
}