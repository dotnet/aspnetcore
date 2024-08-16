// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;
public partial class SendUtilsTests : VerifiableLoggedTest
{
    [Fact]
    public async Task SendMessagesSetsCorrectAcceptHeader()
    {
        var testHttpHandler = new TestHttpMessageHandler();
        var responseTaskCompletionSource = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        testHttpHandler.OnRequest((request, next, cancellationToken) =>
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

        using (var httpClient = new HttpClient(testHttpHandler))
        {
            var pipe = new Pipe();
            var application = new DuplexPipe(pipe.Reader, pipe.Writer);

            // Simulate writing data to send
            await application.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));
            application.Output.Complete();

            await SendUtils.SendMessages(new Uri("http://fakeuri.org"), application, httpClient, logger: Logger).DefaultTimeout();

            var response = await responseTaskCompletionSource.Task.DefaultTimeout();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
