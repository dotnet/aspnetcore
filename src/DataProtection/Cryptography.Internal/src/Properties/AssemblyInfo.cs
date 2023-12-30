// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

// we only ever p/invoke into DLLs known to be in the System32 folder
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

