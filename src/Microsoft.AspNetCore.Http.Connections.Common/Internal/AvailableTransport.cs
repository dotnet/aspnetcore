// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    public class AvailableTransport
    {
        public string Transport { get; set; }
        public List<string> TransferFormats { get; set; }
    }
}
