// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class DisposableActionTest
{
    [Fact]
    public void ActionIsExecutedOnDispose()
    {
        // Arrange
        var called = false;
        var action = new DisposableAction(() => { called = true; });

        // Act
        action.Dispose();

        // Assert
        Assert.True(called, "The action was not run when the DisposableAction was disposed");
    }
}
