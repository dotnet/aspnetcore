// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Internal
{
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
}
