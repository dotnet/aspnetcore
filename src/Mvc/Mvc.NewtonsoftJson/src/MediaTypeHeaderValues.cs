// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

internal static class MediaTypeHeaderValues
{
    public static readonly MediaTypeHeaderValue ApplicationJson
        = MediaTypeHeaderValue.Parse("application/json").CopyAsReadOnly();

    public static readonly MediaTypeHeaderValue TextJson
        = MediaTypeHeaderValue.Parse("text/json").CopyAsReadOnly();

    public static readonly MediaTypeHeaderValue ApplicationJsonPatch
        = MediaTypeHeaderValue.Parse("application/json-patch+json").CopyAsReadOnly();

    public static readonly MediaTypeHeaderValue ApplicationAnyJsonSyntax
        = MediaTypeHeaderValue.Parse("application/*+json").CopyAsReadOnly();
}
