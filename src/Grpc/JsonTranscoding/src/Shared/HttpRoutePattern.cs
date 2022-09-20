// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Grpc.Shared;

internal sealed class HttpRoutePattern
{
    public List<string> Segments { get; }
    public string? Verb { get; }
    public List<HttpRouteVariable> Variables { get; }

    private HttpRoutePattern(List<string> segments, string? verb, List<HttpRouteVariable> variables)
    {
        Segments = segments;
        Verb = verb;
        Variables = variables;
    }

    public static HttpRoutePattern Parse(string pattern)
    {
        var p = new HttpRoutePatternParser(pattern);
        p.Parse();

        return new HttpRoutePattern(p.Segments, p.Verb, p.Variables);
    }
}

internal sealed class HttpRouteVariable
{
    public int StartSegment { get; set; }
    public int EndSegment { get; set; }
    public List<string> FieldPath { get; } = new List<string>();
    public bool HasCatchAllPath { get; set; }
}
