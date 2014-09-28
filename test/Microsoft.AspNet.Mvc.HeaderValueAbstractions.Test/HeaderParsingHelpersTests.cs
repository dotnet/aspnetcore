// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.HeaderValueAbstractions
{
    public class HeaderParsingHelpersTests
    {
        //Accept Headers
        [Fact]
        public void GetAcceptHeaders_ReturnsParsedHeaders()
        {
            // Arrange & Act
            var headers = HeaderParsingHelpers.GetAcceptHeaders("application/xml;q=0.4, application/xhtml;q=0.9");

            // Assert
            Assert.Equal(2, headers.Count);
            Assert.Equal("application", headers[0].MediaType);
            Assert.Equal("xml", headers[0].MediaSubType);
            Assert.Equal(0.4, headers[0].Quality);
            Assert.Equal("application", headers[1].MediaType);
            Assert.Equal("xhtml", headers[1].MediaSubType);
            Assert.Equal(0.9, headers[1].Quality);
        }

        public static IEnumerable<object[]> CasesWhereGetAcceptHeadersReturnsNull
        {
            get
            {
                yield return new object[] { "" };
                yield return new object[] { "application/xml;q=0.4, application/xhtml;q=0,9" };
                yield return new object[] { "application/xml;q=0.4, application/xhtml;q=-0.4" };
                yield return new object[] { "application/xml;q=0 4" };
                yield return new object[] { "application/xml;q=0*4" };
                yield return new object[] { "application/xml;q=1^4" };
            }
        }

        [Theory]
        [MemberData(nameof(CasesWhereGetAcceptHeadersReturnsNull))]
        public void GetAcceptHeaders_ReturnsNull(string acceptHeader)
        {
            // Arrange & Act
            var headers = HeaderParsingHelpers.GetAcceptHeaders(acceptHeader);

            // Assert
            Assert.Null(headers);
        }

        // Charset Headers
        [Fact]
        public void GetAcceptCharsetHeaders_ReturnsParsedHeaders()
        {
            // Arrange & Act
            var headers = HeaderParsingHelpers.GetAcceptCharsetHeaders("utf-8;q=0.7,gzip;q=0.3");

            // Assert
            Assert.Equal(2, headers.Count);
            Assert.Equal("utf-8", headers[0].Value);
            Assert.Equal(0.7, headers[0].Quality);
            Assert.Equal("gzip", headers[1].Value);
            Assert.Equal(0.3, headers[1].Quality);
        }

        public static IEnumerable<object[]> CasesWhereGetCharsetHeadersReturnsNull
        {
            get
            {
                yield return new object[] { "" };
                yield return new object[] { "utf-8;q=0,7,gzip;q=-1" };
                yield return new object[] { "utf-8;q=0*7,gzip;q=1.0" };
                yield return new object[] { "utf-8;q=0 7" };
                yield return new object[] { "utf-8;q=0^7" };
            }
        }

        [Theory]
        [MemberData(nameof(CasesWhereGetCharsetHeadersReturnsNull))]
        public void GetAcceptCharsetHeaders_ReturnsNull_IfAcceptHeaderIsEmpty(string charsetHeader)
        {
            // Arrange & Act
            var headers = HeaderParsingHelpers.GetAcceptCharsetHeaders(charsetHeader);

            // Assert
            Assert.Null(headers);
        }
    }
}