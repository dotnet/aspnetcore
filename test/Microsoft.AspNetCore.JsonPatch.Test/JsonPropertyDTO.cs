// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class JsonPropertyDTO
    {
        [JsonProperty("AnotherName")]
        public string Name { get; set; }
    }
}
