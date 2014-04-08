// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.Cookies
{
    /// <summary>
    /// This default implementation of the ICookieAuthenticationNotifications may be used if the 
    /// application only needs to override a few of the interface methods. This may be used as a base class
    /// or may be instantiated directly.
    /// </summary>
    public class CookieAuthenticationNotifications : ICookieAuthenticationNotifications
    {
        /// <summary>
        /// Create a new instance of the default notifications.
        /// </summary>
        public CookieAuthenticationNotifications()
        {
            OnValidateIdentity = context => Task.FromResult(0);
            OnResponseSignIn = context => { };
            OnResponseSignOut = context => { };
            OnApplyRedirect = DefaultBehavior.ApplyRedirect;
        }

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Func<CookieValidateIdentityContext, Task> OnValidateIdentity { get; set; }

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Action<CookieResponseSignInContext> OnResponseSignIn { get; set; }

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Action<CookieResponseSignOutContext> OnResponseSignOut { get; set; }

        /// <summary>
        /// A delegate assigned to this property will be invoked when the related method is called
        /// </summary>
        public Action<CookieApplyRedirectContext> OnApplyRedirect { get; set; }

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual Task ValidateIdentity(CookieValidateIdentityContext context)
        {
            return OnValidateIdentity.Invoke(context);
        }

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        public virtual void ResponseSignIn(CookieResponseSignInContext context)
        {
            OnResponseSignIn.Invoke(context);
        }

        /// <summary>
        /// Implements the interface method by invoking the related delegate method
        /// </summary>
        /// <param name="context"></param>
        public virtual void ResponseSignOut(CookieResponseSignOutContext context)
        {
            OnResponseSignOut.Invoke(context);
        }

        /// <summary>
        /// Called when a Challenge, SignIn, or SignOut causes a redirect in the cookie middleware
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        public void ApplyRedirect(CookieApplyRedirectContext context)
        {
            OnApplyRedirect.Invoke(context);
        }
    }
}
