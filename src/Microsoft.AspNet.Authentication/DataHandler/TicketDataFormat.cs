// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.DataProtection;

namespace Microsoft.AspNet.Authentication
{
    public class TicketDataFormat : SecureDataFormat<AuthenticationTicket>
    {
        public TicketDataFormat(IDataProtector protector) : base(new TicketSerializer(), protector)
        {
        }
    }
}
