// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.OpenApi;

public static class TestInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Verifier.UseProjectRelativeDirectory(Path.Combine("Integration", "snapshots"));
        VerifierSettings.ScrubLinesContaining(OpenApiConstants.DescriptionId);
    }
}
