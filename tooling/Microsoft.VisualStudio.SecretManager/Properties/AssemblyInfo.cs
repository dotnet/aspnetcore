// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.VisualStudio.Shell;

// required for VS to generate the pkgdef
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Microsoft.VisualStudio.SecretManager.dll")]
[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.VisualStudio.SecretManager",
    GenerateCodeBase = true,
    PublicKeyToken = "adb9793829ddae60",
    OldVersionLowerBound = "0.0.0.0",
    OldVersionUpperBound = "1.0.0.0",
    NewVersion = "1.0.0.0")]
