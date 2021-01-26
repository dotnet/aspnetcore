// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [CollectionDefinition(Name)]
    public class IISCompressionSiteCollection : ICollectionFixture<IISCompressionSiteFixture>
    {
        public const string Name = nameof(IISCompressionSiteCollection);
    }
}
