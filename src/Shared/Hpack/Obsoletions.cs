// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

// Copied from runtime, referenced by shared code.
internal static class Obsoletions
{
    internal const string SharedUrlFormat = "https://aka.ms/dotnet-warnings/{0}";

    internal const string LegacyFormatterImplMessage = "This API supports obsolete formatter-based serialization. It should not be called or extended by application code.";
    internal const string LegacyFormatterImplDiagId = "SYSLIB0051";
}
