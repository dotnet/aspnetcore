// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Extensions.Logging.Testing.Tests
{
    [Repeat]
    public class LoggedTestXunitRepeatTests : LoggedTest
    {
        public static int _runCount = 0;

        [Fact]
        [Repeat(5)]
        public void RepeatLimitIsSetCorrectly()
        {
            Assert.Equal(5, RepeatContext.Limit);
        }

        [Fact]
        [Repeat(5)]
        public void RepeatRunsTestSpecifiedNumberOfTimes()
        {
            Assert.Equal(RepeatContext.CurrentIteration, _runCount);
            _runCount++;
        }

        [Fact]
        public void RepeatCanBeSetOnClass()
        {
            Assert.Equal(10, RepeatContext.Limit);
        }
    }

    public class LoggedTestXunitRepeatAssemblyTests : LoggedTest
    {
        [Fact]
        public void RepeatCanBeSetOnAssembly()
        {
            Assert.Equal(1, RepeatContext.Limit);
        }
    }
}
