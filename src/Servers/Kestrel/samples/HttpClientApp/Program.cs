// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.InternalTesting;

// Console.WriteLine("Ready");
// Console.ReadKey();

var handler = new SocketsHttpHandler();
handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
handler.SslOptions.ClientCertificates = new X509CertificateCollection(new[] { TestResources.GetTestCertificate("eku.client.pfx") });

using var client = new HttpClient(handler);
client.DefaultRequestVersion =
    HttpVersion.Version20;
// HttpVersion.Version30;
client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

var response = await client.GetAsync("https://localhost:5003");
Console.WriteLine(response);
Console.WriteLine(await response.Content.ReadAsStringAsync());

// Alt-svc enables an upgrade after the first request.
response = await client.GetAsync("https://localhost:5003");
Console.WriteLine(response);
Console.WriteLine(await response.Content.ReadAsStringAsync());
