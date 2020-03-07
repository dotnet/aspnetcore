// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.UrlActions;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlActions
{
    public class GoneActionTests
    {
        [Fact]
        public void Gone_Verify410IsInStatusCode()
        {
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var action = new GoneAction();

            action.ApplyAction(context, null, null);

            Assert.Equal(RuleResult.EndResponse, context.Result);
            Assert.Equal(StatusCodes.Status410Gone, context.HttpContext.Response.StatusCode);
        }
    }
}
