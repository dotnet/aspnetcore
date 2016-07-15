// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileStreamResultExecutor : FileResultExecutorBase
    {
        // default buffer size as defined in BufferedStream type
        private const int BufferSize = 0x1000;

        public FileStreamResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<VirtualFileResultExecutor>(loggerFactory))
        {
        }

        public Task ExecuteAsync(ActionContext context, FileStreamResult result)
        {
            SetHeadersAndLog(context, result);
            return WriteFileAsync(context, result);
        }

        private static async Task WriteFileAsync(ActionContext context, FileStreamResult result)
        {
            var response = context.HttpContext.Response;
            var outputStream = response.Body;

            using (result.FileStream)
            {
                await result.FileStream.CopyToAsync(outputStream, BufferSize);
            }
        }
    }
}
