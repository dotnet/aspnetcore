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
