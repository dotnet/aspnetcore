// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Contains the result of an Authenticate call
    /// </summary>
    public class AuthenticateResult
    {
        private AuthenticateResult() { }

        /// <summary>
        /// If a ticket was produced, authenticate was successful.
        /// </summary>
        public bool Succeeded => Ticket != null;

        /// <summary>
        /// The authentication ticket.
        /// </summary>
        public AuthenticationTicket Ticket { get; private set; }

        /// <summary>
        /// Gets the claims-principal with authenticated user identities.
        /// </summary>
        public ClaimsPrincipal Principal => Ticket?.Principal;

        /// <summary>
        /// Additional state values for the authentication session.
        /// </summary>
        public AuthenticationProperties Properties => Ticket?.Properties;

        /// <summary>
        /// Holds failure information from the authentication.
        /// </summary>
        public Exception Failure { get; private set; }

        /// <summary>
        /// Indicates that stage of authentication was directly handled by user intervention and no
        /// further processing should be attempted.
        /// </summary>
        public bool Handled { get; private set; }

        /// <summary>
        /// Indicates that there was no information returned for this authentication scheme.
        /// </summary>
        public bool Nothing { get; private set; }

        /// <summary>
        /// Indicates that authentication was successful.
        /// </summary>
        /// <param name="ticket">The ticket representing the authentication result.</param>
        /// <returns>The result.</returns>
        public static AuthenticateResult Success(AuthenticationTicket ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }
            return new AuthenticateResult() { Ticket = ticket };
        }

        /// <summary>
        /// Indicates that stage of authentication was directly handled by user intervention and no
        /// further processing should be attempted.
        /// </summary>
        /// <returns>The result.</returns>
        public static AuthenticateResult Handle()
        {
            return new AuthenticateResult() { Handled = true };
        }

        /// <summary>
        /// Indicates that there was no information returned for this authentication scheme.
        /// </summary>
        /// <returns>The result.</returns>
        public static AuthenticateResult None()
        {
            return new AuthenticateResult() { Nothing = true };
        }

        /// <summary>
        /// Indicates that there was a failure during authentication.
        /// </summary>
        /// <param name="failure">The failure exception.</param>
        /// <returns>The result.</returns>
        public static AuthenticateResult Fail(Exception failure)
        {
            return new AuthenticateResult() { Failure = failure };
        }

        /// <summary>
        /// Indicates that there was a failure during authentication.
        /// </summary>
        /// <param name="failureMessage">The failure message.</param>
        /// <returns>The result.</returns>
        public static AuthenticateResult Fail(string failureMessage)
        {
            return new AuthenticateResult() { Failure = new Exception(failureMessage) };
        }
    }
}
