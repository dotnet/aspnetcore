// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using Moq;

namespace Microsoft.AspNetCore.Mvc
{
    public static class FlushReportingStream
    {
        public static Stream GetThrowingStream()
        {
            var mock = new Mock<Stream>();
            mock.Verify(m => m.Flush(), Times.Never());
            mock.Verify(m => m.FlushAsync(It.IsAny<CancellationToken>()), Times.Never());

            return mock.Object;
        }
    }
}