// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal enum MSBuildProcessKind
    {
        /// <summary>dotnet msbuild</summary>
        Dotnet,

        /// <summary>msbuild.exe</summary>
        Desktop,
    }
}
