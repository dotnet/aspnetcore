// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class ConnectionEndPointFeatureTests : LoggedTest
{
    [ConditionalFact]
    public async Task Request_ProvidesConnectionEndPointFeature()
    {
        string root;
        EndPoint localEndPoint = null;
        EndPoint remoteEndPoint = null;
        using (Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
        {
            try
            {
                var endPointFeature = httpContext.Features.Get<IConnectionEndPointFeature>();
                localEndPoint = endPointFeature.LocalEndPoint;
                remoteEndPoint = endPointFeature.RemoteEndPoint;
            }
            catch (Exception ex)
            {
                byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                httpContext.Response.Body.Write(body, 0, body.Length);
            }
            return Task.FromResult(0);
        }, options => { }, LoggerFactory))
        {
            string response = await SendRequestAsync(root + "/");
            Assert.Equal(string.Empty, response);
        }

        Assert.NotNull(localEndPoint);
        Assert.NotNull(remoteEndPoint);
        var localIPEndPoint = Assert.IsType<IPEndPoint>(localEndPoint);
        var remoteIPEndPoint = Assert.IsType<IPEndPoint>(remoteEndPoint);
        Assert.NotEqual(0, localIPEndPoint.Port);
        Assert.NotEqual(0, remoteIPEndPoint.Port);
    }

    private async Task<string> SendRequestAsync(string uri)
    {
        using (var client = new System.Net.Http.HttpClient())
        {
            return await client.GetStringAsync(uri);
        }
    }
}
