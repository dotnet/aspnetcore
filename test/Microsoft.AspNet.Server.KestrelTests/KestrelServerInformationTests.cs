// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class KestrelServerInformationTests
    {
        [Fact]
        public void SetThreadCountUsingConfiguration()
        {
            const int expected = 42;

            var values = new Dictionary<string, string>
            {
                { "server.threadCount", expected.ToString() }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var information = new KestrelServerInformation(configuration);

            Assert.Equal(expected, information.ThreadCount);
        }

        [Fact]
        public void SetThreadCountUsingProcessorCount()
        {
            // Ideally we'd mock Environment.ProcessorCount to test edge cases.
            var expected = Clamp(Environment.ProcessorCount >> 1, 1, 16);

            var configuration = new ConfigurationBuilder().Build();

            var information = new KestrelServerInformation(configuration);

            Assert.Equal(expected, information.ThreadCount);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}