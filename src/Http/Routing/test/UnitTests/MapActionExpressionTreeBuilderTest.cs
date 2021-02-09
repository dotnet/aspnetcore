// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class MapActionExpressionTreeBuilderTest
    {
        [Fact]
        public async Task RequestDelegateInvokesAction()
        {
            var invoked = false;
            void TestAction()
            {
                invoked = true;
            };

            var requestDelegate = MapActionExpressionTreeBuilder.BuildRequestDelegate((Action)TestAction);

            await requestDelegate(null!);

            Assert.True(invoked);
        }
    }
}
