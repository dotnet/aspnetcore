// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class FileResultLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _fileResultExecuting;

        static FileResultLoggerExtensions()
        {
            _fileResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing FileResult, sending file as {FileDownloadName}");
        }

        public static void FileResultExecuting(this ILogger logger, string fileDownloadName)
        {
            _fileResultExecuting(logger, fileDownloadName, null);
        }
    }
}
