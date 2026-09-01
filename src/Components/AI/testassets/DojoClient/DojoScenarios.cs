// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoClient;

// Endpoints of AGUIDojoApi that the dojo scenarios post to.
internal static class DojoScenarios
{
    internal const string ApiHttpClientName = "agui-dojo-api";

    internal const string AgenticChatEndpoint = "/agentic_chat";

    internal const string BackendToolRenderingEndpoint = "/backend_tool_rendering";

    internal const string HumanInTheLoopEndpoint = "/human_in_the_loop";

    internal const string ToolBasedGenerativeUIEndpoint = "/tool_based_generative_ui";

    internal const string AgenticGenerativeUIEndpoint = "/agentic_generative_ui";

    internal const string SharedStateEndpoint = "/shared_state";
}
