// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
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
}
