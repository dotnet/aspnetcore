// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class KestrelServerInformationTests
    {
        [Fact]
        public void NullConfigurationThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new KestrelServerInformation(null));
        }

        [Fact]
        public void SetThreadCountUsingConfiguration()
        {
            const int expected = 42;

            var values = new Dictionary<string, string>
            {
                { "kestrel.threadCount", expected.ToString() }
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

        [Fact]
        public void SetAddressesUsingConfiguration()
        {
            var expected = new List<string> { "http://localhost:1337", "https://localhost:42" };

            var values = new Dictionary<string, string>
            {
                { "server.urls", string.Join(";", expected) }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var information = new KestrelServerInformation(configuration);

            Assert.Equal(expected, information.Addresses);
        }

        [Fact]
        public void SetNoDelayUsingConfiguration()
        {
            var values = new Dictionary<string, string>
            {
                { "kestrel.noDelay", "false" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var information = new KestrelServerInformation(configuration);

            Assert.False(information.NoDelay);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("false", false)]
        [InlineData("False", false)]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("Foo", false)]
        [InlineData("FooBar", false)]
        public void SetReuseStreamsUsingConfiguration(string input, bool expected)
        {
            var values = new Dictionary<string, string>
            {
                { "kestrel.reuseStreams", input }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var information = new KestrelServerInformation(configuration);

            Assert.Equal(expected, information.ReuseStreams);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}