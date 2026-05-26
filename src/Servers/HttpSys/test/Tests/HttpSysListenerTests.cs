// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class HttpSysListenerTests
{
    [Fact]
    public void CreateHttpInitializeFailureException_WithErrorCode_IncludesWin32ExceptionDetails()
    {
        var exception = HttpSysListener.CreateHttpInitializeFailureException(ErrorCodes.ERROR_ACCESS_DENIED);

        Assert.Contains("status code 0x00000005", exception.Message);
        Assert.Contains("HRESULT 0x80070005", exception.Message);

        var win32Exception = Assert.IsType<Win32Exception>(exception.InnerException);
        Assert.Equal((int)ErrorCodes.ERROR_ACCESS_DENIED, win32Exception.NativeErrorCode);
    }

    [Fact]
    public void CreateHttpInitializeFailureException_WithSuccessCode_ReturnsDefaultPlatformNotSupportedException()
    {
        var exception = HttpSysListener.CreateHttpInitializeFailureException(ErrorCodes.ERROR_SUCCESS);

        Assert.Null(exception.InnerException);
    }
}
