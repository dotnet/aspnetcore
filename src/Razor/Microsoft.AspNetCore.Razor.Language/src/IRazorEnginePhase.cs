// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public interface IRazorEnginePhase
{
    RazorEngine Engine { get; set; }

    void Execute(RazorCodeDocument codeDocument);
}
