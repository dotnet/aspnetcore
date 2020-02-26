// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3LimitsTests
    {
        [Fact]
        public void CannotUpdateDynamicTableSettings()
        {
            var limits = new Http3Limits();
            Assert.Throws<NotImplementedException>(() => limits.BlockedStreams = 1);
            Assert.Throws<NotImplementedException>(() => limits.HeaderTableSize = 1);
        }
    }
}
