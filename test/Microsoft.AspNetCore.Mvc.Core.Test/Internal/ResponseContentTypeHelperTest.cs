// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ResponseContentTypeHelperTest
    {
        public static TheoryData<MediaTypeHeaderValue, string, string> ResponseContentTypeData
        {
            get
            {
                // contentType, responseContentType, expectedContentType
                return new TheoryData<MediaTypeHeaderValue, string, string>
                {
                    // No explicit content type is provided, fall-back to the default content type
                    {
                        null,
                        null,
                        "text/default; p1=p1-value; charset=utf-8"
                    },

                    // Content type is set explicitly without encoding on action result. No charset parameter added in
                    // expected content type
                    {
                        new MediaTypeHeaderValue("text/foo"),
                        null,
                        "text/foo"
                    },

                    // Content type is set explicitly with encoding on action result. Expected content type
                    // has the charset
                    {
                        MediaTypeHeaderValue.Parse("text/foo; charset=us-ascii"),
                        null,
                        "text/foo; charset=us-ascii"
                    },

                    // Content type is set explicitly without encoding and additional parameters on action result
                    // Expected content type has the additional parameters but with no charset.
                    {
                        MediaTypeHeaderValue.Parse("text/foo; p1=p1-value"),
                        null,
                        "text/foo; p1=p1-value"
                    },

                    // Content type is set explicitly with encoding and additional parameters on action result
                    // Expected content type has the additional parameters and the charset.
                    {
                        MediaTypeHeaderValue.Parse("text/foo; p1=p1-value; charset=us-ascii"),
                        null,
                        "text/foo; p1=p1-value; charset=us-ascii"
                    },

                    // Content type is set explicitly without encoding on http response.
                    // No charset parameter added in expected content type
                    {
                        null,
                        "text/bar",
                        "text/bar"
                    },

                    // Content type is set explicitly without encoding and additional parameters on http response
                    // No charset parameter added in expected content type
                    {
                        null,
                        "text/bar; p1=p1-value",
                        "text/bar; p1=p1-value"
                    },

                    // Content type is set explicitly with encoding and additional parameters on http response
                    // Expected content type has charset and additional parameters
                    {
                        null,
                        "text/bar; p1=p1-value; charset=us-ascii",
                        "text/bar; p1=p1-value; charset=us-ascii"
                    },

                    // Content type set on action result takes precedence over the content type set on http response
                    {
                        MediaTypeHeaderValue.Parse("text/foo; charset=us-ascii"),
                        "text/bar",
                        "text/foo; charset=us-ascii"
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo; charset=us-ascii"),
                        "text/bar; charset=utf-8",
                        "text/foo; charset=us-ascii"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ResponseContentTypeData))]
        public void GetsExpectedContentTypeAndEncoding(
            MediaTypeHeaderValue contentType,
            string responseContentType,
            string expectedContentType)
        {
            // Arrange
            var defaultContentType = "text/default; p1=p1-value; charset=utf-8";

            // Act
            string resolvedContentType = null;
            Encoding resolvedContentTypeEncoding = null;
            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                contentType?.ToString(),
                responseContentType,
                defaultContentType,
                out resolvedContentType,
                out resolvedContentTypeEncoding);

            // Assert
            Assert.Equal(expectedContentType, resolvedContentType);
        }

        [Fact]
        public void DoesNotThrowException_OnInvalidResponseContentType()
        {
            // Arrange
            var expectedContentType = "invalid-content-type";
            var defaultContentType = "text/plain; charset=utf-8";

            // Act
            string resolvedContentType = null;
            Encoding resolvedContentTypeEncoding = null;
            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                null,
                expectedContentType,
                defaultContentType,
                out resolvedContentType,
                out resolvedContentTypeEncoding);

            // Assert
            Assert.Equal(expectedContentType, resolvedContentType);
            Assert.Equal(Encoding.UTF8, resolvedContentTypeEncoding);
        }
    }
}
