// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class HelperChunk : ChunkBlock
    {
        public LocationTagged<string> Signature { get; set; }
        public LocationTagged<string> Footer { get; set; }
        public bool HeaderComplete { get; set; }
    }
}
