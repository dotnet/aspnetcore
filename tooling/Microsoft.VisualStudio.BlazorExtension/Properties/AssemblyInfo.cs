// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;

// Add binding redirects for each assembly we ship in VS. This is required so that these assemblies show
// up in the Load context, which means that we can use ServiceHub and other nice things.
//
// The versions here need to match what the build is producing. If you change the version numbers
// for the Blazor assemblies, this needs to change as well.
[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.VisualStudio.LanguageServices.Blazor",
    GenerateCodeBase = true,
    PublicKeyToken = "",
    OldVersionLowerBound = "0.0.0.0",
    OldVersionUpperBound = "0.0.5.0",
    NewVersion = "0.0.5.0")]
