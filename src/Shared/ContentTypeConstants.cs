// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static class ContentTypeConstants
{
    public const string JsonContentType = "application/json";
    internal static IEnumerable<string> ApplicationJsonContentTypes { get; } = ["application/json"];
    public const string ProblemDetailsContentType = "application/problem+json";
    public static readonly IEnumerable<string> ProblemDetailsContentTypes = [ProblemDetailsContentType];
    public const string JsonContentTypeWithCharset = "application/json; charset=utf-8";
    public const string BinaryContentType = "application/octet-stream";
    public const string DefaultContentType = "text/plain; charset=utf-8";
}
