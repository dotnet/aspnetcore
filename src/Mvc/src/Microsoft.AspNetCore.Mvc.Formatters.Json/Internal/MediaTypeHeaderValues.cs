// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json.Internal
{
    internal class MediaTypeHeaderValues
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
}
