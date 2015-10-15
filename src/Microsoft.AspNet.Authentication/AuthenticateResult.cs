// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Authentication
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
        public bool Succeeded
        {
            get
            {
                return Ticket != null;
            }
        }

        /// <summary>
        /// The authentication ticket.
        /// </summary>
        public AuthenticationTicket Ticket { get; private set; }

        /// <summary>
        /// Holds error information caused by authentication.
        /// </summary>
        public Exception Error { get; private set; }

        public static AuthenticateResult Success(AuthenticationTicket ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }
            return new AuthenticateResult() { Ticket = ticket };
        }

        public static AuthenticateResult Failed(Exception error)
        {
            return new AuthenticateResult() { Error = error };
        }

        public static AuthenticateResult Failed(string errorMessage)
        {
            return new AuthenticateResult() { Error = new Exception(errorMessage) };
        }

    }
}
