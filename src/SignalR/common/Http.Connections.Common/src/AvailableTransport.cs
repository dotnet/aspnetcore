// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Stores the <see cref="Transport"/> and <see cref="TransferFormats"/> supported by the Transport.
    /// Used in the <see cref="NegotiationResponse"/> to tell the client what is supported.
    /// </summary>
    public class AvailableTransport
    {
        /// <summary>
        /// The name of the Transport.
        /// </summary>
        public string Transport { get; set; }
        /// <summary>
        /// The formats supported by this <see cref="Transport"/>.
        /// </summary>
        public IList<string> TransferFormats { get; set; }
    }
}
