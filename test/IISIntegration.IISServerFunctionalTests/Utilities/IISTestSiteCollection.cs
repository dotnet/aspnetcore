// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    /// <summary>
    /// This type just maps collection names to available fixtures
    /// </summary>
    [CollectionDefinition(Name)]
    public class IISTestSiteCollection : ICollectionFixture<IISTestSiteFixture>
    {
        public const string Name = nameof(IISTestSiteCollection);
    }
}
