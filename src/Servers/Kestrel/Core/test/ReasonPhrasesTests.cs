// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ReasonPhraseTests
    {
        [Theory]
        [InlineData(999, "Unknown", "999 Unknown")]
        [InlineData(999, null, "999 ")]
        [InlineData(StatusCodes.Status200OK, "OK", "200 OK")]
        [InlineData(StatusCodes.Status200OK, null, "200 OK")]
        [InlineData(StatusCodes.Status200OK, "Custom OK", "200 Custom OK")]
        public void Formatting(int statusCode, string reasonPhrase, string expectedResult)
        {
            var bytes = Internal.Http.ReasonPhrases.ToStatusBytes(statusCode, reasonPhrase);
            Assert.NotNull(bytes);
            Assert.Equal(expectedResult, Encoding.ASCII.GetString(bytes));
        }

        [Fact]
        public void CachesKnownPhrases()
        {
            for (var statusCode = 1; statusCode < 1000; statusCode++)
            {
                var reasonPhrase = WebUtilities.ReasonPhrases.GetReasonPhrase(statusCode);

                if (!string.IsNullOrEmpty(reasonPhrase))
                {
                    var bytes = Internal.Http.ReasonPhrases.ToStatusBytes(statusCode, reasonPhrase);
                    var bytesCached = Internal.Http.ReasonPhrases.ToStatusBytes(statusCode);

                    Assert.Same(bytes, bytesCached);
                    Assert.EndsWith(reasonPhrase, Encoding.ASCII.GetString(bytes));
                }
            }
        }
    }
}
