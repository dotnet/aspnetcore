// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Security.Provider;

namespace Microsoft.AspNet.Security.Infrastructure
{
    public class AuthenticationTokenCreateContext : BaseContext
    {
        private readonly ISecureDataFormat<AuthenticationTicket> _secureDataFormat;

        public AuthenticationTokenCreateContext(
            HttpContext context,
            ISecureDataFormat<AuthenticationTicket> secureDataFormat,
            AuthenticationTicket ticket)
            : base(context)
        {
            if (secureDataFormat == null)
            {
                throw new ArgumentNullException("secureDataFormat");
            }
            if (ticket == null)
            {
                throw new ArgumentNullException("ticket");
            }
            _secureDataFormat = secureDataFormat;
            Ticket = ticket;
        }

        public string Token { get; protected set; }

        public AuthenticationTicket Ticket { get; protected set; }

        public string SerializeTicket()
        {
            return _secureDataFormat.Protect(Ticket);
        }

        public void SetToken(string tokenValue)
        {
            if (tokenValue == null)
            {
                throw new ArgumentNullException("tokenValue");
            }
            Token = tokenValue;
        }
    }
}
