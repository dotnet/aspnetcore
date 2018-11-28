// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;

// Add binding redirects for each assembly we ship in VS. This is required so that these assemblies show
// up in the Load context, which means that we can use ServiceHub and other nice things.
//
// The versions here need to match what the build is producing. If you change the version numbers
// for the referenced assemblies, this needs to change as well.
[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.AspNetCore.Components.Razor.Extensions",
    GenerateCodeBase = true,
    PublicKeyToken = "",
    OldVersionLowerBound = "0.0.0.0",
    OldVersionUpperBound = "3.0.0.0",
    NewVersion = "3.0.0.0")]
[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.VisualStudio.LanguageServices.Blazor",
    GenerateCodeBase = true,
    PublicKeyToken = "",
    OldVersionLowerBound = "0.0.0.0",
    OldVersionUpperBound = "0.8.0.0",
    NewVersion = "0.8.0.0")]
[assembly: ProvideBindingRedirection(
    AssemblyName = "AngleSharp",
    PublicKeyToken = "",
    OldVersionLowerBound = "0.0.0.0",
    OldVersionUpperBound = "0.9.9.0",
    NewVersion = "0.9.9.0")]