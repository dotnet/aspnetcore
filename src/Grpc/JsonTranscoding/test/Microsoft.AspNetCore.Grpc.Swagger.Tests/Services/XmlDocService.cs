// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.Core;
using Microsoft.Extensions.Logging;
using Xmldoc;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;

public class XmlDocService : XmlDoc.XmlDocBase
{
    private readonly ILogger _logger;

    public XmlDocService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<XmlDocServiceWithComments>();
    }

    public override Task<StringReply> BasicGet(StringRequest request, ServerCallContext context)
    {
        return base.BasicGet(request, context);
    }

    public override Task<StringReply> BodyRootPost(StringRequestWithDetail request, ServerCallContext context)
    {
        return base.BodyRootPost(request, context);
    }

    public override Task<StringReply> BodyPathPost(StringRequestWithDetail request, ServerCallContext context)
    {
        return base.BodyPathPost(request, context);
    }
}
