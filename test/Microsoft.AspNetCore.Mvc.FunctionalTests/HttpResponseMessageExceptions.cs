// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FiltersWebSite;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public static class HttpResponseMessageExceptions
    {
        public static ExceptionInfo GetServerException(this HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new AssertActualExpectedException(
                    HttpStatusCode.InternalServerError,
                    response.StatusCode,
                    "A server-side exception should be returned as a 500.");
            }

            var headers = response.Headers;

            IEnumerable<string> exceptionMessageHeader;
            IEnumerable<string> exceptionTypeHeader;
            if (!headers.TryGetValues(ErrorReporterMiddleware.ExceptionMessageHeader, out exceptionMessageHeader))
            {
                throw new XunitException(
                    "No value for the '" + ErrorReporterMiddleware.ExceptionMessageHeader + "' header.");
            }

            if (!headers.TryGetValues(ErrorReporterMiddleware.ExceptionTypeHeader, out exceptionTypeHeader))
            {
                throw new XunitException(
                    "No value for the '" + ErrorReporterMiddleware.ExceptionTypeHeader + "' header.");
            }

            return new ExceptionInfo()
            {
                ExceptionMessage = Assert.Single(exceptionMessageHeader),
                ExceptionType = Assert.Single(exceptionTypeHeader),
            };
        }
    }
}