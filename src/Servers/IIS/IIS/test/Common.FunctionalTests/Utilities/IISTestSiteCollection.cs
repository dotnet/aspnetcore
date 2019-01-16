// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    /// <summary>
    /// This type just maps collection names to available fixtures
    /// </summary>
    [CollectionDefinition(Name)]
    public class IISTestSiteCollection : ICollectionFixture<IISTestSiteFixture>
    {
        public const string Name = nameof(IISTestSiteCollection);
    }

    [CollectionDefinition(Name)]
    public class OutOfProcessTestSiteCollection : ICollectionFixture<OutOfProcessTestSiteFixture>
    {
        public const string Name = nameof(OutOfProcessTestSiteCollection);
    }

    [CollectionDefinition(Name)]
    public class OutOfProcessV1TestSiteCollection : ICollectionFixture<OutOfProcessV1TestSiteFixture>
    {
        public const string Name = nameof(OutOfProcessV1TestSiteCollection);
    }

}
