// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Tests;

public static class ExceptionMessageExtensions
{
    public static string GetLocalizationSafeMessage(this ArgumentException argEx)
    {
        // Strip off the last line since it's "Parameter Name: [parameterName]" and:
        // 1. We verify the parameter name separately
        // 2. It is localized, so we don't want our tests to break in non-US environments
        var message = argEx.Message;
        var lastNewline = message.LastIndexOf(" (Parameter", StringComparison.Ordinal);
        if (lastNewline < 0)
        {
            return message;
        }

        return message.Substring(0, lastNewline);
    }
}
