// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

internal static class HttpExtensions
{
    private const string UrlEncodedFormContentType = "application/x-www-form-urlencoded";
    private const string MultipartFormContentType = "multipart/form-data";

    internal static bool IsValidHttpMethodForForm(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    internal static bool HasApplicationFormContentType([NotNullWhen(true)] string? contentType)
    {
        // Content-Type: application/x-www-form-urlencoded; charset=utf-8
        return contentType != null && contentType.Contains(UrlEncodedFormContentType, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool HasMultipartFormContentType([NotNullWhen(true)] string? contentType)
    {
        // Content-Type: multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq
        return contentType != null && contentType.Contains(MultipartFormContentType, StringComparison.OrdinalIgnoreCase);
    }
}
