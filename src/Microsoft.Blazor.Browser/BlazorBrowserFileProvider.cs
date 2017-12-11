// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Internal.Common.FileProviders;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Blazor.Browser
{
    public static class BlazorBrowserFileProvider
    {
        public static IFileProvider Instance = new EmbeddedResourceFileProvider(
            typeof(BlazorBrowserFileProvider).Assembly,
            "blazor.");
    }
}
