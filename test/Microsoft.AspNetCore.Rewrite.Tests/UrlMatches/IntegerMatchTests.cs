// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.AspNetCore.Rewrite.Internal.UrlActions;
using Microsoft.AspNetCore.Rewrite.Internal.UrlMatches;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlMatches
{
    public class IntegerMatchTests
    {
        [Fact]
        public void IntegerMatch_Constructor_Integer_Parse_Excetion()
        {
            var ex = Assert.Throws<FormatException>(() => new IntegerMatch("Not an int", IntegerOperationType.Equal));
            Assert.Equal(ex.Message, Resources.Error_IntegerMatch_FormatExceptionMessage);
        }

        [Theory]
        [InlineData(1,IntegerOperationType.Equal,"1",true)]
        [InlineData(1, IntegerOperationType.NotEqual, "2", true)]
        [InlineData(2, IntegerOperationType.Less, "1", true)]
        [InlineData(1, IntegerOperationType.LessEqual, "2", false)]
        [InlineData(1, IntegerOperationType.Greater, "2", true)]
        [InlineData(2, IntegerOperationType.GreaterEqual, "1", false)]
        [InlineData(1, IntegerOperationType.Equal, "Not an int", false)]
        [InlineData(1, IntegerOperationType.Equal, "", false)]
        [InlineData(1, IntegerOperationType.Equal, "2147483648", false)]
        public void IntegerMatch_Evaluation_Cases_Tests(int value,IntegerOperationType operation, string input,bool expectedResult)
        {
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var integerMatch = new IntegerMatch(value, operation);
            var matchResult = integerMatch.Evaluate(input, context);
            Assert.Equal(expectedResult, matchResult.Success);
        }
    }
}
