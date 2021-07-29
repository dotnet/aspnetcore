// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;

var handler = new SocketsHttpHandler();
handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;

using var client = new HttpClient(handler);
client.DefaultRequestVersion = HttpVersion.Version20;
client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

var response = await client.GetAsync("https://localhost:5001");
Console.WriteLine(response);

// Alt-svc enables an upgrade after the first request.
response = await client.GetAsync("https://localhost:5001");
Console.WriteLine(response);
