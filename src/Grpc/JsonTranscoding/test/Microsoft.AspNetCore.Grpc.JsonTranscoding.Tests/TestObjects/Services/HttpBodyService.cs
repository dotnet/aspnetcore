// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Google.Api;
using Google.Protobuf;
using Grpc.Core;
using Transcoding;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.TestObjects;

public class HttpBodyService : Transcoding.HttpBodyService.HttpBodyServiceBase
{
    public override Task<HttpBody> HelloWorld(HelloWorldRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HttpBody
        {
            ContentType = "application/xml",
            Data = ByteString.CopyFrom(Encoding.UTF8.GetBytes(@"<message>Hello world</message>"))
        });
    }
}
