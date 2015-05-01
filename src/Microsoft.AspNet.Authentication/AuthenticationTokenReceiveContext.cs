// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Notifications;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authentication
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
