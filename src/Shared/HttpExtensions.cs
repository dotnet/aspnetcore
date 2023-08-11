// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Http;

internal static class HttpExtensions
{
    internal static string UrlEncodedFormContentType = "application/x-www-form-urlencoded";
    internal static string MultipartFormContentType = "multipart/form-data";

    internal static bool IsValidHttpMethodForForm(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    internal static bool IsValidContentTypeForForm(string? contentType)
    {
        if (contentType == null)
        {
            return false;
        }

        // Abort early if this doesn't look like it could be a form-related content-type

        if (contentType.Length < 19)
        {
            return false;
        }

        return contentType.Equals(UrlEncodedFormContentType, StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith(MultipartFormContentType, StringComparison.OrdinalIgnoreCase);
    }
}
