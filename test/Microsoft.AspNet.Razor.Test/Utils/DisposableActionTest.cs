// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using Microsoft.AspNet.Razor.Utils;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Utils
{
    public class DisposableActionTest
    {
        [Fact]
        public void ConstructorRequiresNonNullAction()
        {
            Assert.Throws<ArgumentNullException>("action", () => new DisposableAction(null));
        }

        [Fact]
        public void ActionIsExecutedOnDispose()
        {
            // Arrange
            bool called = false;
            DisposableAction action = new DisposableAction(() => { called = true; });

            // Act
            action.Dispose();

            // Assert
            Assert.True(called, "The action was not run when the DisposableAction was disposed");
        }
    }
}
