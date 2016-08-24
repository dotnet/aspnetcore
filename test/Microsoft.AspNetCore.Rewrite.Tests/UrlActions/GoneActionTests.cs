// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.AspNetCore.Rewrite.Internal.UrlActions;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlActions
{
    public class GoneActionTests
    {
        [Fact]
        public void Gone_Verify410IsInStatusCode()
        {
            // Arrange
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var action = new GoneAction();

            // Act
            var results = action.ApplyAction(context, null, null);

            // Assert
            Assert.Equal(results.Result, RuleTermination.ResponseComplete);
            Assert.Equal(context.HttpContext.Response.StatusCode, StatusCodes.Status410Gone);
        }
    }
}
