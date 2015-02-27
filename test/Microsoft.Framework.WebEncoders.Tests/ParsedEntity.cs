// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Framework.WebEncoders
{
    internal sealed class ParsedEntity
    {
        [JsonProperty("codepoints")]
        public int[] Codepoints { get; set; }

        [JsonProperty("characters")]
        public string DecodedString { get; set; }
    }
}
