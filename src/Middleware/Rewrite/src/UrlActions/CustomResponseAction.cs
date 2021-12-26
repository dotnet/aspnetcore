// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite.UrlActions;

internal class CustomResponseAction : UrlAction
{
    public int StatusCode { get; }
    public string? StatusReason { get; set; }
    public string? StatusDescription { get; set; }

    public CustomResponseAction(int statusCode)
    {
        StatusCode = statusCode;
    }

    public override void ApplyAction(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCode;

        if (!string.IsNullOrEmpty(StatusReason))
        {
            context.HttpContext.Features.GetRequiredFeature<IHttpResponseFeature>().ReasonPhrase = StatusReason;
        }

        if (!string.IsNullOrEmpty(StatusDescription))
        {
            var feature = context.HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (feature != null)
            {
                feature.AllowSynchronousIO = true;
            }
            var content = Encoding.UTF8.GetBytes(StatusDescription);
            response.ContentLength = content.Length;
            response.ContentType = "text/plain; charset=utf-8";
            response.Body.Write(content, 0, content.Length);
        }

        context.Result = RuleResult.EndResponse;

        context.Logger.CustomResponse(context.HttpContext.Request.GetEncodedUrl());
    }
}
