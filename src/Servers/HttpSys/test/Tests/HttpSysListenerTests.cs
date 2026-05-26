// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class HttpSysListenerTests
{
    [Fact]
    public void CreateHttpInitializeFailureException_WithErrorCode_ReturnsHttpSysExceptionWithDetails()
    {
        var errorCode = ErrorCodes.ERROR_ACCESS_DENIED;
        var exception = Assert.IsType<HttpSysException>(HttpSysListener.CreateHttpInitializeFailureException(errorCode));
        var expectedHResult = HttpSysListener.CreateHResultFromWin32Error(errorCode);

        Assert.Contains($"status code 0x{errorCode:X8}", exception.Message);
        Assert.Contains($"HRESULT 0x{expectedHResult:X8}", exception.Message);
        Assert.Equal((int)errorCode, exception.NativeErrorCode);
    }

    [Fact]
    public void CreateHttpInitializeFailureException_WithSuccessCode_ReturnsExceptionWithoutInnerException()
    {
        var exception = HttpSysListener.CreateHttpInitializeFailureException(ErrorCodes.ERROR_SUCCESS);

        Assert.IsType<PlatformNotSupportedException>(exception);
        Assert.Null(exception.InnerException);
    }
}
