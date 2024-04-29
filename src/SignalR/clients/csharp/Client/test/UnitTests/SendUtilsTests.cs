// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Moq;
using Moq.Protected;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;
public partial class SendUtilsTests : VerifiableLoggedTest
{
    [Fact]
    public async Task SendMessagesSetsCorrectAcceptHeader()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var responseTaskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();

        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                if (request.Headers.Accept?.Contains(new MediaTypeWithQualityHeaderValue("*/*")) == true)
                {
                    responseTaskCompletionSource.SetResult(ResponseUtils.CreateResponse(HttpStatusCode.OK));
                }
                else
                {
                    responseTaskCompletionSource.SetResult(ResponseUtils.CreateResponse(HttpStatusCode.BadRequest));
                }
                return responseTaskCompletionSource.Task;
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        using (StartVerifiableLog())
        {
            var pipe = new Pipe();
            var application = new DuplexPipe(pipe.Reader, pipe.Writer);

            // Simulate writing data to send
            await application.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));

            application.Output.Complete();

            await SendUtils.SendMessages(new Uri("http://fakeuri.org"), application, httpClient, logger: Logger).DefaultTimeout();

            var response = await responseTaskCompletionSource.Task;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
