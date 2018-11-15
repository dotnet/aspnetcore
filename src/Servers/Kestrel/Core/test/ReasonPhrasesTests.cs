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
        public void Formatting(int statusCode, string reasonPhrase, string expectedResult)
        {
            var bytes = Internal.Http.ReasonPhrases.ToStatusBytes(statusCode, reasonPhrase);
            Assert.NotNull(bytes);
            Assert.Equal(expectedResult, Encoding.ASCII.GetString(bytes));
        }
    }
}