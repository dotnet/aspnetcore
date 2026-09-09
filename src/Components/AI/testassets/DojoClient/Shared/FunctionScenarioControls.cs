// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoClient;

internal sealed class FunctionScenarioControls(Func<string, Task> releaseResult)
{
    internal Task ReleaseResultAsync(string threadId) => releaseResult(threadId);
}
