// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.BlazorExtension
{
    // We mainly have a package so we can have an "About" dialog entry.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [AboutDialogInfo(PackageGuidString, "ASP.NET Core Blazor Language Services", "#110", "112")]
    [Guid(BlazorPackage.PackageGuidString)]
    public sealed class BlazorPackage : Package
    {
        public const string PackageGuidString = "d9fe04bc-57a7-4107-915e-3a5c2f9e19fb";
    }
}
