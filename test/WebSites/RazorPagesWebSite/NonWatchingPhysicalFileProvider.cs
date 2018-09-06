// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace RazorPagesWebSite
{
    public class NonWatchingPhysicalFileProvider : PhysicalFileProvider, IFileProvider
    {
        public NonWatchingPhysicalFileProvider(string root) : base(root)
        {
        }

        IChangeToken IFileProvider.Watch(string filter) => throw new ArgumentException("This method should not be called.");
    }
}
