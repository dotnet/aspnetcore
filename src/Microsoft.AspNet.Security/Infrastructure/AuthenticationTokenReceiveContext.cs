// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Security.Notifications;

namespace Microsoft.AspNet.Security.Infrastructure
{
    public class AuthenticationTokenReceiveContext : BaseContext
    {
        private readonly ISecureDataFormat<AuthenticationTicket> _secureDataFormat;

        public AuthenticationTokenReceiveContext(
            [NotNull] HttpContext context,
            [NotNull] ISecureDataFormat<AuthenticationTicket> secureDataFormat,
            [NotNull] string token)
            : base(context)
        {
            _secureDataFormat = secureDataFormat;
            Token = token;
        }

        public string Token { get; protected set; }

        public AuthenticationTicket Ticket { get; protected set; }

        public void DeserializeTicket(string protectedData)
        {
            Ticket = _secureDataFormat.Unprotect(protectedData);
        }

        public void SetTicket([NotNull] AuthenticationTicket ticket)
        {
            Ticket = ticket;
        }
    }
}
