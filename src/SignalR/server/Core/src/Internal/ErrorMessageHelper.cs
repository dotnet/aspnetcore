// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static class ErrorMessageHelper
{
    internal static string BuildErrorMessage(string message, Exception exception, bool includeExceptionDetails)
    {
        if (exception is HubException || includeExceptionDetails)
        {
            return $"{message} {exception.GetType().Name}: {exception.Message}";
        }

        return message;
    }
}
