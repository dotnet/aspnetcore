// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    [TestCaseOrderer("Microsoft.AspNetCore.Testing.AlphabeticalOrderer", "Microsoft.AspNetCore.Testing.Tests")]
    public class ConditionalFactTest : IClassFixture<ConditionalFactTest.ConditionalFactAsserter>
    {
        public ConditionalFactTest(ConditionalFactAsserter collector)
        {
            Asserter = collector;
        }

        private ConditionalFactAsserter Asserter { get; }

        [Fact]
        public void TestAlwaysRun()
        {
            // This is required to ensure that the type at least gets initialized.
            Assert.True(true);
        }

        [ConditionalFact(Skip = "Test is always skipped.")]
        public void ConditionalFactSkip()
        {
            Assert.True(false, "This test should always be skipped.");
        }

#if NETCOREAPP
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
        public void ThisTestMustRunOnCoreCLR()
        {
            Asserter.TestRan = true;
        }
#elif NET472
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public void ThisTestMustRunOnCLR()
        {
            Asserter.TestRan = true;
        }
#else
#error Target frameworks need to be updated.
#endif

        // Test is named this way to be the lowest test in the alphabet, it relies on test ordering
        [Fact]
        public void ZzzzzzzEnsureThisIsTheLastTest()
        {
            Assert.True(Asserter.TestRan);
        }

        public class ConditionalFactAsserter : IDisposable
        {
            public bool TestRan { get; set; }

            public void Dispose()
            {
            }
        }
    }
}
