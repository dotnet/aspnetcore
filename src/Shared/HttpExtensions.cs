// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Http;

internal static class HttpExtensions
{
    internal const string UrlEncodedFormContentType = "application/x-www-form-urlencoded";
    internal const string MultipartFormContentType = "multipart/form-data";

    internal static bool IsValidHttpMethodForForm(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    internal static bool IsValidContentTypeForForm(string? contentType)
    {
        if (contentType == null)
        {
            return false;
        }

        // Abort early if this doesn't look like it could be a form-related content-type

        if (contentType.Length < MultipartFormContentType.Length)
        {
            return false;
        }

        return contentType.Equals(UrlEncodedFormContentType, StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith(MultipartFormContentType, StringComparison.OrdinalIgnoreCase);
    }
}
