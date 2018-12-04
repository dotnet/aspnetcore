using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal.UrlMatches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlMatches
{
    public class StringMatchTests
    {
        [Theory]
        [InlineData("hi", StringOperationType.Equal,true,"hi",true)]
        [InlineData("a", StringOperationType.Greater, true, "b", true)]
        [InlineData("a", StringOperationType.GreaterEqual, true, "b", true)]
        [InlineData("b", StringOperationType.Less,true, "a", true)]
        [InlineData("b", StringOperationType.LessEqual, true, "a", true)]
        [InlineData("", StringOperationType.Equal, true, "", true)]
        [InlineData(null, StringOperationType.Equal, true, null, true)]
        public void StringMatch_Evaluation_Check_Cases(string value, StringOperationType operation, bool ignoreCase, string input, bool expectedResult)
        {
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var stringMatch = new StringMatch(value, operation, ignoreCase);
            var matchResult = stringMatch.Evaluate(input, context);
            Assert.Equal(expectedResult, matchResult.Success);
        }
    }
}
