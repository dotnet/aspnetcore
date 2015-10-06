// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Filter;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Https;
using Microsoft.AspNet.Testing.xunit;
using Xunit;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class HttpsConnectionFilterTests
    {
        private async Task App(IFeatureCollection frame)
        {
            var request = frame.Get<IHttpRequestFeature>();
            var response = frame.Get<IHttpResponseFeature>();
            response.Headers.Clear();
            while (true)
            {
                var buffer = new byte[8192];
                var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }
                await response.Body.WriteAsync(buffer, 0, count);
            }
        }

        // https://github.com/aspnet/KestrelHttpServer/issues/240
        // This test currently fails on mono because of an issue with SslStream.
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task CanReadAndWriteWithHttpsConnectionFilter()
        {
            RemoteCertificateValidationCallback validationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += validationCallback;

                var sereverAddress = "https://localhost:54321/";
                var serviceContext = new TestServiceContext()
                {
                    ConnectionFilter = new HttpsConnectionFilter(
                        new X509Certificate2(@"TestResources/testCert.pfx", "testPassword"),
                        new NoOpConnectionFilter())
                };

                using (var server = new TestServer(App, serviceContext, sereverAddress))
                {
                    using (var client = new HttpClient())
                    {
                        var result = await client.PostAsync(sereverAddress, new FormUrlEncodedContent(new[] {
                            new KeyValuePair<string, string>("content", "Hello World?")
                        }));

                        Assert.Equal("content=Hello+World%3F", await result.Content.ReadAsStringAsync());
                    }
                }
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback -= validationCallback;
            }
        }
    }
}
#endif
