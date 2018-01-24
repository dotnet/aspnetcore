// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Internal.Common.FileProviders;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Blazor.Mono
{
    public static class MonoStaticFileProvider
    {
        public readonly static IFileProvider JsFiles = new EmbeddedResourceFileProvider(
            typeof(MonoStaticFileProvider).Assembly, "mono.js.");

        public readonly static IFileProvider BclFiles = new EmbeddedResourceFileProvider(
            typeof(MonoStaticFileProvider).Assembly, "mono.bcl.");
    }
}
