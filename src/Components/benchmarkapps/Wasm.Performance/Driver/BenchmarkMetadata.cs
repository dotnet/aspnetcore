// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Wasm.Performance.Driver
{
    internal class BenchmarkMetadata
    {
        public string Source { get; set; }
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }
        public string Format { get; set; }
    }
}
