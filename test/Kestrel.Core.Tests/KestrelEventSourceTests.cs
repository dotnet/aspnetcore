// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class KestrelEventSourceTests
    {
        [Fact]
        public void ExistsWithCorrectId()
        {
            var esType = typeof(KestrelServer).GetTypeInfo().Assembly.GetType(
                "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelEventSource",
                throwOnError: true,
                ignoreCase: false
            );

            Assert.NotNull(esType);

            Assert.Equal("Microsoft-AspNetCore-Server-Kestrel", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("bdeb4676-a36e-5442-db99-4764e2326c7d"), EventSource.GetGuid(esType));
            Assert.NotEmpty(EventSource.GenerateManifest(esType, "assemblyPathToIncludeInManifest"));
        }
    }
}
