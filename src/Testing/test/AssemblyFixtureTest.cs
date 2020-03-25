// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    // We include a collection and assembly fixture to verify that they both still work.
    [Collection("MyCollection")]
    [TestCaseOrderer("Microsoft.AspNetCore.Testing.AlphabeticalOrderer", "Microsoft.AspNetCore.Testing.Tests")]
    public class AssemblyFixtureTest
    {
        public AssemblyFixtureTest(TestAssemblyFixture assemblyFixture, TestCollectionFixture collectionFixture)
        {
            AssemblyFixture = assemblyFixture;
            CollectionFixture = collectionFixture;
        }

        public TestAssemblyFixture AssemblyFixture { get; }
        public TestCollectionFixture CollectionFixture { get; }

        [Fact]
        public void A()
        {
            Assert.NotNull(AssemblyFixture);
            Assert.Equal(0, AssemblyFixture.Count);

            Assert.NotNull(CollectionFixture);
            Assert.Equal(0, CollectionFixture.Count);

            AssemblyFixture.Count++;
            CollectionFixture.Count++;
        }

        [Fact]
        public void B()
        {
            Assert.Equal(1, AssemblyFixture.Count);
            Assert.Equal(1, CollectionFixture.Count);
        }
    }

    [CollectionDefinition("MyCollection", DisableParallelization = true)]
    public class MyCollection : ICollectionFixture<TestCollectionFixture>
    {
    }
}
