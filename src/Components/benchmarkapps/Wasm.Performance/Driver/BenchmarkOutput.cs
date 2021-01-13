// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Wasm.Performance.Driver
{
    internal class BenchmarkOutput
    {
        public List<BenchmarkMetadata> Metadata { get; } = new List<BenchmarkMetadata>();

        public List<BenchmarkMeasurement> Measurements { get; } = new List<BenchmarkMeasurement>();
    }
}
