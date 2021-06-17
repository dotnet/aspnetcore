// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class SystemTextJsonIResultExecutorTest : SystemTextJsonResultExecutorTestBase
    {
        protected override IActionResultExecutor<JsonResult> CreateExecutor(ILoggerFactory loggerFactory)
        {
            return new JsonIResultToActionResultExecutorAdapter();
        }

        protected override bool LogsSuccess => false;
        protected override bool SerializesWithContentTypeEncoding => false;

        private class JsonIResultToActionResultExecutorAdapter : IActionResultExecutor<JsonResult>
        {
            public Task ExecuteAsync(ActionContext context, JsonResult result) =>
                ((IResult)result).ExecuteAsync(context.HttpContext);
        }
    }
}
