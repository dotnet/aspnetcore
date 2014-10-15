// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !ASPNETCORE50

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.AspNet.PipelineCore;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNet.Mvc.WebApiCompatShimTest
{
    public class HttpResponseMessageOutputFormatterTests
    {
        [Fact]
        public async Task Disposed_CalledOn_HttpResponseMessage()
        {
            // Arrange
            var formatter = new HttpResponseMessageOutputFormatter();
            var streamContent = new Mock<StreamContent>(new MemoryStream());
            streamContent.Protected().Setup("Dispose", true).Verifiable();
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Content = streamContent.Object;
            var outputFormatterContext = GetOutputFormatterContext(httpResponseMessage, typeof(HttpResponseMessage));

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            streamContent.Protected().Verify("Dispose", Times.Once(), true);
        }

        private OutputFormatterContext GetOutputFormatterContext(object outputValue, Type outputType)
        {
            return new OutputFormatterContext
            {
                Object = outputValue,
                DeclaredType = outputType,
                ActionContext = new ActionContext(new DefaultHttpContext(), routeData: null, actionDescriptor: null)
            };
        }
    }
}
#endif