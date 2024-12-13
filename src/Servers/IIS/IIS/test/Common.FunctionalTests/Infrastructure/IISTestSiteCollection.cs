// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

/// <summary>
/// This type just maps collection names to available fixtures
/// </summary>
[CollectionDefinition(Name)]
public class IISTestSiteCollectionInProc : ICollectionFixture<IISTestSiteFixture>
{
    public const string Name = nameof(IISTestSiteCollectionInProc);
}

[CollectionDefinition(Name)]
public class IISTestSiteCollectionOutOfProc : ICollectionFixture<IISTestSiteFixture>
{
    public const string Name = nameof(IISTestSiteCollectionOutOfProc);
}

[CollectionDefinition(Name)]
public class IISHttpsTestSiteCollection : ICollectionFixture<IISTestSiteFixture>
{
    public const string Name = nameof(IISHttpsTestSiteCollection);
}
