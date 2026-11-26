// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop;

public class JSExceptionTest
{
    [Fact]
    public void MessageConstructorDoesNotSetName()
    {
        var exception = new JSException("Failure");

        Assert.Equal("Failure", exception.Message);
        Assert.Null(exception.Name);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void InnerExceptionConstructorPreservesCause()
    {
        var cause = new InvalidOperationException("Cause");
        var exception = new JSException("Failure", cause);

        Assert.Equal("Failure", exception.Message);
        Assert.Null(exception.Name);
        Assert.Same(cause, exception.InnerException);
    }

    [Fact]
    public void NullInnerExceptionRemainsUnambiguous()
    {
        var exception = new JSException("Failure", null!);

        Assert.Equal("Failure", exception.Message);
        Assert.Null(exception.Name);
        Assert.Null(exception.InnerException);
    }
}
