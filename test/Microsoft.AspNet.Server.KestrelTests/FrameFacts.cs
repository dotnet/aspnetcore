// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Http;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class FrameFacts
    {
        [Fact]
        public void ResetResetsScheme()
        {
            // Arrange
            var frame = new Frame(new ConnectionContext() { DateHeaderValueManager = new DateHeaderValueManager() });
            frame.Scheme = "https";

            // Act
            frame.Reset();

            // Assert
            Assert.Equal("http", frame.Get<IHttpRequestFeature>().Scheme);
        }
    }
}
