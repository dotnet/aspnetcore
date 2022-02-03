// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc;

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
