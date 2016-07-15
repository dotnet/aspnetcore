// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileContentResultExecutor : FileResultExecutorBase
    {
        public FileContentResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<FileContentResultExecutor>(loggerFactory))
        {
        }

        public Task ExecuteAsync(ActionContext context, FileContentResult result)
        {
            SetHeadersAndLog(context, result);
            return WriteFileAsync(context, result);
        }

        private static Task WriteFileAsync(ActionContext context, FileContentResult result)
        {
            var response = context.HttpContext.Response;

            return response.Body.WriteAsync(result.FileContents, offset: 0, count: result.FileContents.Length);
        }
    }
}
