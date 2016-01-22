// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class ForbidResultLoggerExtensions
    {
        private static readonly Action<ILogger, string[], Exception> _resultExecuting =
            LoggerMessage.Define<string[]>(
                LogLevel.Information,
                eventId: 1,
                formatString: $"Executing {nameof(ForbidResult)} with authentication schemes ({{Schemes}}).");

        public static void ForbidResultExecuting(this ILogger logger, IList<string> authenticationSchemes)
        {
            _resultExecuting(logger, authenticationSchemes.ToArray(), null);
        }
    }
}
