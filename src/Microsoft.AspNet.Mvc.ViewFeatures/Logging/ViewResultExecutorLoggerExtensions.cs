// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class ViewResultExecutorLoggerExtensions
    {
        private static Action<ILogger, string, Exception> _viewResultExecuting;

        static ViewResultExecutorLoggerExtensions()
        {
            _viewResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing ViewResult, running view at path {Path}.");
        }

        public static void ViewResultExecuting(this ILogger logger, IView view)
        {
            _viewResultExecuting(logger, view.Path, null);
        }
    }
}
