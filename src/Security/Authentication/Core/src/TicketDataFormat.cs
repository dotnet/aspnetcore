// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// A <see cref="SecureDataFormat{TData}"/> instance to secure
    /// <see cref="AuthenticationTicket"/>.
    /// </summary>
    public class TicketDataFormat : SecureDataFormat<AuthenticationTicket>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TicketDataFormat"/>.
        /// </summary>
        /// <param name="protector">The <see cref="IDataProtector"/>.</param>
        public TicketDataFormat(IDataProtector protector)
            : base(TicketSerializer.Default, protector)
        {
        }
    }
}
