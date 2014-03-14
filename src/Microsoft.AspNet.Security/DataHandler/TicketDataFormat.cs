// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Security.DataHandler.Encoder;
using Microsoft.AspNet.Security.DataHandler.Serializer;
using Microsoft.AspNet.Security.DataProtection;

namespace Microsoft.AspNet.Security.DataHandler
{
    public class TicketDataFormat : SecureDataFormat<AuthenticationTicket>
    {
        public TicketDataFormat(IDataProtector protector) : base(DataSerializers.Ticket, protector, TextEncodings.Base64Url)
        {
        }
    }
}
