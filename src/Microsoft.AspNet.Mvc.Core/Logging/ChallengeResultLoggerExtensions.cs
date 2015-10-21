// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class ChallengeResultLoggerExtenstions
    {
        private static readonly Action<ILogger, string[], Exception> _challengeResultExecuting;

        static ChallengeResultLoggerExtenstions()
        {
            _challengeResultExecuting = LoggerMessage.Define<string[]>(
                LogLevel.Information,
                1,
                "Executing ChallengeResult with authentication schemes ({Schemes}).");
        }

        public static void ChallengeResultExecuting(this ILogger logger, IList<string> schemes)
        {
            _challengeResultExecuting(logger, schemes.ToArray(), null);
        }
    }
}
