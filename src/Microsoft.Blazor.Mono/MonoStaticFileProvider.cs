// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Internal.Common.FileProviders;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Blazor.Mono
{
    public static class MonoStaticFileProvider
    {
        public readonly static IFileProvider Instance = new EmbeddedResourceFileProvider(
            typeof(MonoStaticFileProvider).Assembly, "mono.");
    }
}
