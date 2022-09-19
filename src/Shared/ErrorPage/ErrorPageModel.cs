// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.StackTrace.Sources;

namespace Microsoft.AspNetCore.Hosting.Views;

/// <summary>
/// Holds data to be displayed on the error page.
/// </summary>
internal sealed class ErrorPageModel
{
    public ErrorPageModel(IEnumerable<ExceptionDetails> errorDetails, bool showRuntimeDetails, string runtimeDisplayName, string runtimeArchitecture, string clrVersion, string currentAssemblyVesion, string operatingSystemDescription)
    {
        ErrorDetails = errorDetails;
        ShowRuntimeDetails = showRuntimeDetails;
        RuntimeDisplayName = runtimeDisplayName;
        RuntimeArchitecture = runtimeArchitecture;
        ClrVersion = clrVersion;
        CurrentAssemblyVesion = currentAssemblyVesion;
        OperatingSystemDescription = operatingSystemDescription;
    }

    /// <summary>
    /// Detailed information about each exception in the stack.
    /// </summary>
    public IEnumerable<ExceptionDetails> ErrorDetails { get; }

    public bool ShowRuntimeDetails { get; }

    public string RuntimeDisplayName { get; }

    public string RuntimeArchitecture { get; }

    public string ClrVersion { get; }

    public string CurrentAssemblyVesion { get; }

    public string OperatingSystemDescription { get; }
}
