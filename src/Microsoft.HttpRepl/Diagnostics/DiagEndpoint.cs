// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.HttpRepl.Diagnostics
{
    public class DiagEndpoint
    {
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public DiagEndpointMetadata[] Metadata { get; set; }
    }
}
