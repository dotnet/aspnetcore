// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Internal;

namespace Microsoft.AspNetCore.Hosting
{
    internal interface ISupportsStartup
    {
        IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure);
        IWebHostBuilder UseStartup([DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)] Type startupType);
        IWebHostBuilder UseStartup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]TStartup>(Func<WebHostBuilderContext, TStartup> startupFactory);
    }
}
