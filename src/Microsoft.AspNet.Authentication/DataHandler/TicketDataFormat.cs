// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Authentication.DataHandler.Encoder;
using Microsoft.AspNet.Authentication.DataHandler.Serializer;

namespace Microsoft.AspNet.Authentication.DataHandler
{
    public class TicketDataFormat : SecureDataFormat<AuthenticationTicket>
    {
        public TicketDataFormat(IDataProtector protector) : base(DataSerializers.Ticket, protector, TextEncodings.Base64Url)
        {
        }
    }
}
