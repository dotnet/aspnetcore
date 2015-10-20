// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class PartialViewResultExecutorLoggerExtensions
    {
        private static Action<ILogger, string, Exception> _partialViewResultExecuting;

        static PartialViewResultExecutorLoggerExtensions()
        {
            _partialViewResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing PartialViewResult, running view at path {Path}.");
        }

        public static void PartialViewResultExecuting(this ILogger logger, IView view)
        {
            _partialViewResultExecuting(logger, view.Path, null);
        }
    }
}
