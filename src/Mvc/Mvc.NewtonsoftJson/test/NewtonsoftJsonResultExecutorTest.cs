// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson
{
    public class NewtonsoftJsonResultExecutorTest : JsonResultExecutorTestBase
    {
        protected override IActionResultExecutor<JsonResult> CreateExecutor(ILoggerFactory loggerFactory)
        {
            return new NewtonsoftJsonResultExecutor(
                new TestHttpResponseStreamWriterFactory(),
                loggerFactory.CreateLogger< NewtonsoftJsonResultExecutor>(),
                Options.Create(new MvcOptions()),
                Options.Create(new MvcNewtonsoftJsonOptions()),
                ArrayPool<char>.Shared);
        }

        protected override object GetIndentedSettings()
        {
            return new JsonSerializerSettings { Formatting = Formatting.Indented };
        }
    }
}
