// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoClient;

// The DojoClient-only HTTP client name used to reach AGUIDojoApi. The scenario endpoint paths
// themselves are shared with AGUIDojoApi through DojoAgent.DojoScenarioEndpoints.
internal static class DojoScenarios
{
    internal const string ApiHttpClientName = "agui-dojo-api";
}
