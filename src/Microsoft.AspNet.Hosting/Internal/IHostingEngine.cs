// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Hosting.Internal
{
    public interface IHostingEngine
    {
        IApplication Start();

        // Accessing this will block Use methods
        IServiceProvider ApplicationServices { get; }
    }
}