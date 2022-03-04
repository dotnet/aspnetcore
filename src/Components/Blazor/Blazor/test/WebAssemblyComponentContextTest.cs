// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Blazor.Services.Test
{
    public class WebAssemblyComponentContextTest
    {
        [Fact]
        public void IsConnected()
        {
            Assert.True(new WebAssemblyComponentContext().IsConnected);
        }
    }
}
