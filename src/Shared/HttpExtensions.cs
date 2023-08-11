// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

internal static class HttpExtensions
{
    internal static bool IsValidHttpMethodForForm(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    internal static bool IsValidContentTypeForForm(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var mediaType) &&
        (mediaType.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ||
        mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase));
}
