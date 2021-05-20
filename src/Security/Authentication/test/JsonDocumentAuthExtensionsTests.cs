// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Test
{
    public class JsonDocumentAuthExtensionsTests
    {
        [Theory]
        [InlineData("{ \"foo\": null }", null)]
        [InlineData("{ \"foo\": \"\" }", "")]
        [InlineData("{ \"foo\": \"bar\" }", "bar")]
        [InlineData("{ \"foo\": 1 }", "1")]
        [InlineData("{ \"bar\": \"baz\" }", null)]
        public void GetStringReturnsCorrectValue(string json, string expected)
        {
            using (var document = JsonDocument.Parse(json))
            {
                var value = document.RootElement.GetString("foo");
                Assert.Equal(expected, value);
            }
        }
    }
}
