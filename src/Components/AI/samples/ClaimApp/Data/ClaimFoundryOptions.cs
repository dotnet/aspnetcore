// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComponentsAIClaimApp.Data;

internal sealed class ClaimFoundryOptions
{
    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string VisionModel { get; set; } = "gpt-5-mini";

    public string TranscriptionModel { get; set; } = "gpt-4o-mini-transcribe";

    public string ResearchCountry { get; set; } = "US";
}
