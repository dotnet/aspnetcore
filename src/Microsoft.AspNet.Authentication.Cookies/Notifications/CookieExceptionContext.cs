// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Notifications;

namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// Context object passed to the ICookieAuthenticationProvider method Exception.
    /// </summary>    
    public class CookieExceptionContext : BaseContext<CookieAuthenticationOptions>
    {
        /// <summary>
        /// Creates a new instance of the context object.
        /// </summary>
        /// <param name="context">The HTTP request context</param>
        /// <param name="options">The middleware options</param>
        /// <param name="location">The location of the exception</param>
        /// <param name="exception">The exception thrown.</param>
        /// <param name="ticket">The current ticket, if any.</param>
        public CookieExceptionContext(
            HttpContext context,
            CookieAuthenticationOptions options,
            ExceptionLocation location,
            Exception exception,
            AuthenticationTicket ticket)
            : base(context, options)
        {
            Location = location;
            Exception = exception;
            Rethrow = true;
            Ticket = ticket;
        }

        /// <summary>
        /// The code paths where exceptions may be reported.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Scope = "type",
            Target = "Microsoft.Owin.Security.Cookies.CookieExceptionContext+ExceptionLocation", Justification = "It is a directly related option.")]
        public enum ExceptionLocation
        {
            /// <summary>
            /// The exception was reported in the Authenticate code path.
            /// </summary>
            Authenticate,

            /// <summary>
            /// The exception was reported in the ApplyResponseGrant code path, during sign-in, sign-out, or refresh.
            /// </summary>
            ApplyResponseGrant,

            /// <summary>
            /// The exception was reported in the ApplyResponseChallenge code path, during redirect generation.
            /// </summary>
            ApplyResponseChallenge,
        }

        /// <summary>
        /// The code path the exception occurred in.
        /// </summary>
        public ExceptionLocation Location { get; private set; }

        /// <summary>
        /// The exception thrown.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// True if the exception should be re-thrown (default), false if it should be suppressed. 
        /// </summary>
        public bool Rethrow { get; set; }

        /// <summary>
        /// The current authentication ticket, if any.
        /// In the AuthenticateAsync code path, if the given exception is not re-thrown then this ticket
        /// will be returned to the application. The ticket may be replaced if needed.
        /// </summary>
        public AuthenticationTicket Ticket { get; set; }
    }
}
