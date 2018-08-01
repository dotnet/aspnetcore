// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.OpenApi
{
    public interface IEndpointMetadataReader
    {
        bool CanHandle(JObject document);

        IEnumerable<EndpointMetadata> ReadMetadata(JObject document);
    }
}
