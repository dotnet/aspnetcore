// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Wasm.Performance.Driver
{
    internal class BenchmarkMeasurement
    {
        public DateTime Timestamp { get; internal set; }
        public string Name { get; internal set; }
        public double Value { get; internal set; }
    }
}