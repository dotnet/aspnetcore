// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class NegotiationResponse
    {
        public string Url { get; set; }
        public string AccessToken { get; set; }
        public string ConnectionId { get; set; }
        public string ConnectionToken { get; set; }
        public int Version { get; set; }
        public IList<AvailableTransport> AvailableTransports { get; set; }
        public string Error { get; set; }
    }
}
