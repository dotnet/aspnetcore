// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="ComNetOS.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Permissions;

namespace Microsoft.AspNet.Security.Windows
{
    internal static class ComNetOS
    {
        // Minimum support for Windows 2008 is assumed.
        internal static readonly bool IsWin7orLater;  // Is Windows 7 or later
        internal static readonly bool IsWin8orLater;  // Is Windows 8 or later

        // We use it safe so assert
        [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.AppDomain, ResourceScope.AppDomain)]
        static ComNetOS()
        {
            OperatingSystem operatingSystem = Environment.OSVersion;

            GlobalLog.Print("ComNetOS::.ctor(): " + operatingSystem.ToString());

            Debug.Assert(operatingSystem.Platform != PlatformID.Win32Windows, "Windows 9x is not supported");

            var Win7Version = new Version(6, 1);
            var Win8Version = new Version(6, 2);
            IsWin7orLater = (operatingSystem.Version >= Win7Version);
            IsWin8orLater = (operatingSystem.Version >= Win8Version);
        }
    }
}
