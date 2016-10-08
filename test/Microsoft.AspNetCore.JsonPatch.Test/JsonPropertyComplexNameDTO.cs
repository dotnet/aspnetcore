// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class JsonPropertyComplexNameDTO
    {
        [JsonProperty("foo/bar~")]
        public string FooSlashBars { get; set; }

        [JsonProperty("foo/~")]
        public SimpleDTO FooSlashTilde { get; set; }
    }
}
