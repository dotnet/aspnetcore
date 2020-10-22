// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.WebSockets.Microbenchmarks
{
    public class HandshakeBenchmark
    {
        private string[] _requestKeys = {
            "F8/qpj9RYr2/sIymdDvlmw==",
            "PyQi8nyMkKnI7JKiAJ/IrA==",
            "CUe0z8ItSBRtgJlPqP1+SQ==",
            "w9vo1A9oM56M31qPQYKL6g==",
            "+vqFGD9U04QOxKdWHrduTQ==",
            "xsfuh2ZOm5O7zTzFPWJGUA==",
            "TvmUzr4DgBLcDYX88kEAyw==",
            "EZ5tcEOxWm7tF6adFXLSQg==",
            "bkmoBhqwbbRzL8H9hvH1tQ==",
            "EUwBrmmwivd5czsxz9eRzQ==",
        };

        [Benchmark(OperationsPerInvoke = 10)]
        public void CreateResponseKey()
        {
            foreach (var key in _requestKeys)
            {
                HandshakeHelpers.CreateResponseKey(key);
            }
        }

        [Benchmark(OperationsPerInvoke = 10)]
        public void IsRequestKeyValid()
        {
            foreach (var key in _requestKeys)
            {
                HandshakeHelpers.IsRequestKeyValid(key);
            }
        }
    }
}
