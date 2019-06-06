// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ConnectionContextTests
    {
        [Fact]
        public void ParameterlessAbortCreateConnectionAbortedException()
        {
            var mockConnectionContext = new Mock<ConnectionContext> { CallBase = true };
            ConnectionAbortedException ex = null;

            mockConnectionContext.Setup(c => c.Abort(It.IsAny<ConnectionAbortedException>()))
                                 .Callback<ConnectionAbortedException>(abortReason => ex = abortReason);

            mockConnectionContext.Object.Abort();

            Assert.NotNull(ex);
            Assert.Equal("The connection was aborted by the application via ConnectionContext.Abort().", ex.Message);
        }
    }
}
