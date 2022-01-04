// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RazorWebSite;

public class TaskReturningService
{
    public async Task<string> GetValueAsync()
    {
        await Task.Delay(100);
        return "Value from TaskReturningString";
    }
}
