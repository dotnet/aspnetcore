// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Razor.Utils;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Utils
{
    public class DisposableActionTest
    {
        [Fact]
        public void ConstructorRequiresNonNullAction()
        {
            Assert.ThrowsArgumentNull(() => new DisposableAction(null), "action");
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
