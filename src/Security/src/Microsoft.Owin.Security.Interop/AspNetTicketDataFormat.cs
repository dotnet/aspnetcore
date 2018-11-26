// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataHandler.Encoder;
using Microsoft.Owin.Security.DataProtection;

namespace Microsoft.Owin.Security.Interop
{
    public class AspNetTicketDataFormat : SecureDataFormat<AuthenticationTicket>
    {
        public AspNetTicketDataFormat(IDataProtector protector)
            : base(AspNetTicketSerializer.Default, protector, TextEncodings.Base64Url)
        {
        }
    }
}