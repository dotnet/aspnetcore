// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;

namespace TestContentPackage;

public sealed record ActivityLinksTestSpan(
    string Name,
    string TraceId,
    string SpanId,
    string? Route,
    string? CircuitId,
    ActivityLinksTestLink[] Links)
{
    public static ActivityLinksTestSpan FromActivity(Activity activity)
        => new(
            activity.OperationName,
            activity.TraceId.ToString(),
            activity.SpanId.ToString(),
            activity.GetTagItem("aspnetcore.components.route") as string,
            activity.GetTagItem("aspnetcore.components.circuit.id") as string,
            [.. activity.Links.Select(link => new ActivityLinksTestLink(
                link.Context.TraceId.ToString(),
                link.Context.SpanId.ToString()))]);
}

public sealed record ActivityLinksTestLink(string TraceId, string SpanId);
