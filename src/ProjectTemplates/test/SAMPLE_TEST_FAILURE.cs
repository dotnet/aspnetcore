// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace ProjectTemplates.Tests
{
    public class SAMPLE_TEST_FAILURE
    {
        [Fact]
        public void FailingTest()
        {
            Assert.False(true, "Should have been false.");
        }
    }
}
