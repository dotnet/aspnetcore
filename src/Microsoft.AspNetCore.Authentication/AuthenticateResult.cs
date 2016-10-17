// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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
        /// Holds failure information from the authentication.
        /// </summary>
        public Exception Failure { get; private set; }

        /// <summary>
        /// Indicates that stage of authentication was directly handled by user intervention and no
        /// further processing should be attempted.
        /// </summary>
        public bool Handled { get; private set; }

        /// <summary>
        /// Indicates that this stage of authentication was skipped by user intervention.
        /// </summary>
        public bool Skipped { get; private set; }

        public static AuthenticateResult Success(AuthenticationTicket ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }
            return new AuthenticateResult() { Ticket = ticket };
        }

        public static AuthenticateResult Handle()
        {
            return new AuthenticateResult() { Handled = true };
        }

        public static AuthenticateResult Skip()
        {
            return new AuthenticateResult() { Skipped = true };
        }

        public static AuthenticateResult Fail(Exception failure)
        {
            return new AuthenticateResult() { Failure = failure };
        }

        public static AuthenticateResult Fail(string failureMessage)
        {
            return new AuthenticateResult() { Failure = new Exception(failureMessage) };
        }

    }
}
