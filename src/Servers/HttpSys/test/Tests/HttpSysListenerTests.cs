// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class HttpSysListenerTests
{
    [Fact]
    public void CreateHttpInitializeFailureException_WithErrorCode_IncludesHttpSysExceptionDetails()
    {
        var errorCode = ErrorCodes.ERROR_ACCESS_DENIED;
        var exception = HttpSysListener.CreateHttpInitializeFailureException(errorCode);
        var expectedWin32Exception = new Win32Exception((int)errorCode);

        Assert.Contains($"status code 0x{errorCode:X8}", exception.Message);
        Assert.Contains($"HRESULT 0x{expectedWin32Exception.HResult:X8}", exception.Message);

        var httpSysException = Assert.IsType<HttpSysException>(exception.InnerException);
        Assert.Equal((int)errorCode, httpSysException.NativeErrorCode);
    }

    [Fact]
    public void CreateHttpInitializeFailureException_WithSuccessCode_ReturnsExceptionWithoutInnerException()
    {
        var exception = HttpSysListener.CreateHttpInitializeFailureException(ErrorCodes.ERROR_SUCCESS);

        Assert.Null(exception.InnerException);
    }
}
