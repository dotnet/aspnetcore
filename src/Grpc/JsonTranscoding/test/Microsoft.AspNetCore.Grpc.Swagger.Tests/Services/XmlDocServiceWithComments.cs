// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.Core;
using Microsoft.Extensions.Logging;
using Xmldoc;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;

/// <summary>
/// XmlDocServiceWithComments XML comment!
/// </summary>
public class XmlDocServiceWithComments : XmlDoc.XmlDocBase
{
    private readonly ILogger _logger;

    public XmlDocServiceWithComments(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<XmlDocServiceWithComments>();
    }

    /// <summary>
    /// BasicGet XML summary!
    /// </summary>
    /// <remarks>
    /// BasicGet XML remarks!
    /// </remarks>
    /// <param name="request">Request XML comment!</param>
    /// <param name="context"></param>
    /// <response code="200">Returns the newly created item!</response>
    /// <response code="404">Not found!</response>
    /// <returns>Returns comment!</returns>
    public override Task<StringReply> BasicGet(StringRequest request, ServerCallContext context)
    {
        return base.BasicGet(request, context);
    }

    /// <summary>
    /// BodyRootPost XML summary!
    /// </summary>
    /// <param name="request">Request XML param!</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task<StringReply> BodyRootPost(StringRequestWithDetail request, ServerCallContext context)
    {
        return base.BodyRootPost(request, context);
    }
}
