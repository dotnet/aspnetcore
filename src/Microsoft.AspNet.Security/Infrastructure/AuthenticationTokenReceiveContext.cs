// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Notifications;

namespace Microsoft.AspNet.Security.Infrastructure
{
    public class AuthenticationTokenReceiveContext : BaseContext
    {
        public AuthenticationTokenReceiveContext(
            [NotNull] HttpContext context,
            [NotNull] string token)
            : base(context)
        {
            Token = token;
        }

        public string Token { get; protected set; }

        public AuthenticationTicket Ticket { get; protected set; }

        public void SetTicket([NotNull] AuthenticationTicket ticket)
        {
            Ticket = ticket;
        }
    }
}
