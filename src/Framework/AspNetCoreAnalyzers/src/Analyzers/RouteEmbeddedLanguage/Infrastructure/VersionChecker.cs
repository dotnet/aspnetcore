// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class VersionChecker
{
    private static readonly Version MinVersion = new Version(4, 300, 22, 32601);

    private static bool? _isSupported;
    public static bool IsSupported
    {
        get
        {
            if (_isSupported == null)
            {
                var fvi = FileVersionInfo.GetVersionInfo(typeof(SemanticModelAnalysisContext).Assembly.Location);
                var fileVersion = new Version(fvi.FileVersion);

                _isSupported = fileVersion >= MinVersion;
            }

            return _isSupported.Value;
        }
    }
}
