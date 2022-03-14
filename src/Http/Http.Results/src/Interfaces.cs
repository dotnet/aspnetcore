// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

public interface IStatusCodeHttpResult
{
   public int? StatusCode { get; }
}

public interface IObjectHttpResult
{
    public object? Value { get; }
}

public interface IProblemHttpResult
{
    public ProblemDetails ProblemDetails { get; }
    public string ContentType { get; }
}

public interface IAtRouteHttpResult
{
   public string? RouteName { get; }
   public RouteValueDictionary? RouteValues { get; }
}

public interface IAtLocationHttpResult
{
   public string? Location { get; }
}

public interface IContentHttpResult
{
   public string? Content { get; }
   public string? ContentType { get; }
}

public interface IRedirectHttpResult
{
   public bool AcceptLocalUrlOnly { get; }
   public bool Permanent { get; }
   public bool PreserveMethod { get; }
   public string? Url { get; }
}

public interface IFileHttpResult
{
   public string ContentType { get; }
   public string? FileDownloadName { get; }
   public DateTimeOffset? LastModified { get; }
   public EntityTagHeaderValue? EntityTag { get; }
   public bool EnableRangeProcessing { get; }
   public long? FileLength { get; }
}
