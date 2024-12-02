// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

// We include a collection and assembly fixture to verify that they both still work.
[Collection("MyCollection")]
[TestCaseOrderer("Microsoft.AspNetCore.InternalTesting.AlphabeticalOrderer", "Microsoft.AspNetCore.InternalTesting.Tests")]
public class AssemblyFixtureTest
{
    public AssemblyFixtureTest(
        TestAssemblyFixture assemblyFixture,
        TestCollectionFixture collectionFixture)
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
