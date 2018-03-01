// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Blazor.Razor;
using Microsoft.AspNetCore.Razor.Language;

[assembly: ProvideRazorExtensionInitializer("Blazor-0.1", typeof(BlazorExtensionInitializer))]
[assembly: ProvideRazorExtensionInitializer("BlazorDeclaration-0.1", typeof(BlazorExtensionInitializer))]

[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Blazor.Build.Test")]
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Blazor.Razor.Extensions.Test")]