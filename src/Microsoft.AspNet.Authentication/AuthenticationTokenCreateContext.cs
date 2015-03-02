// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Notifications;

namespace Microsoft.AspNet.Authentication
{
    public class AuthenticationTokenCreateContext : BaseContext
    {
        private readonly ISecureDataFormat<AuthenticationTicket> _secureDataFormat;

        public AuthenticationTokenCreateContext(
            [NotNull] HttpContext context,
            [NotNull] ISecureDataFormat<AuthenticationTicket> secureDataFormat,
            [NotNull] AuthenticationTicket ticket)
            : base(context)
        {
            _secureDataFormat = secureDataFormat;
            Ticket = ticket;
        }

        public string Token { get; protected set; }

        public AuthenticationTicket Ticket { get; protected set; }

        public string SerializeTicket()
        {
            return _secureDataFormat.Protect(Ticket);
        }

        public void SetToken([NotNull] string tokenValue)
        {
            Token = tokenValue;
        }
    }
}
