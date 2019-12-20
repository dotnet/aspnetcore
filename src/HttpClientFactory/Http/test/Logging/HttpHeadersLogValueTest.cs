// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Xunit;

namespace Microsoft.Extensions.Http.Logging
{
    public class HttpHeadersLogValueTest
    {
        [Fact]
        public void HttpHeadersLogValue_ToString_HidesOnlyLogSensitiveHeadersValue()
        {
            // Arrange
            var headers = new TestHttpHeaders
            {
                { "secureHeader1", "value1" },
                { "unsecureHeader1", "value1" }
            };
            var contentHeaders = new TestHttpHeaders
            {
                { "unsecureHeader2", "value2" },
                { "secureHeader2", "value2" }
            };
            var headersToRedact = new HashSet<string>
            {
                "secureHeader1",
                "secureHeader2",
            };
            var sensitiveHeaders = new HashSet<string>(headersToRedact, StringComparer.OrdinalIgnoreCase);

            Func<string, bool> shouldRedactHeaderValue = (header) => sensitiveHeaders.Contains(header);

            var httpHeadersLogValue = new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Request, headers, contentHeaders, shouldRedactHeaderValue);

            // Act
            var result = httpHeadersLogValue.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(
                "Request Headers:" + Environment.NewLine +
                "secureHeader1: *" + Environment.NewLine +
                "unsecureHeader1: value1" + Environment.NewLine +
                "unsecureHeader2: value2" + Environment.NewLine +
                "secureHeader2: *" + Environment.NewLine,
                result);
        }

        private class TestHttpHeaders : HttpHeaders { }
    }
}
