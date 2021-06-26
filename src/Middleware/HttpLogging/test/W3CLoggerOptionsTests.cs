// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class W3CLoggerOptionsTests
    {
        [Fact]
        public void DoesNotInitializeWithOptionalFields()
        {
            var options = new W3CLoggerOptions();
            // Optional fields shouldn't be logged by default
            Assert.False(options.LoggingFields.HasFlag(W3CLoggingFields.UserName));
            Assert.False(options.LoggingFields.HasFlag(W3CLoggingFields.Cookie));
        }

        [Fact]
        public void ThrowsOnNegativeFileSizeLimit()
        {
            var options = new W3CLoggerOptions();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.FileSizeLimit = -1);
            Assert.Contains("FileSizeLimit must be positive", ex.Message);
        }

        [Fact]
        public void ThrowsOnEmptyFileName()
        {
            var options = new W3CLoggerOptions();
            Assert.Throws<ArgumentNullException>(() => options.FileName = "");
        }

        [Fact]
        public void ThrowsOnEmptyLogDirectory()
        {
            var options = new W3CLoggerOptions();
            Assert.Throws<ArgumentNullException>(() => options.LogDirectory = "");
        }

        [Fact]
        public void ThrowsOnNegativeFlushInterval()
        {
            var options = new W3CLoggerOptions();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.FlushInterval = TimeSpan.FromSeconds(-1));
            Assert.Contains("FlushInterval must be positive", ex.Message);
        }
    }
}
