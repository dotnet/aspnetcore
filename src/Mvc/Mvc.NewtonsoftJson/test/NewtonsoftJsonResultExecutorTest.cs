// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO;
using Microsoft.AspNetCore.Http;
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
                GetFileBufferingStreamFactory(),
                loggerFactory.CreateLogger<NewtonsoftJsonResultExecutor>(),
                Options.Create(new MvcOptions()),
                Options.Create(new MvcNewtonsoftJsonOptions()),
                ArrayPool<char>.Shared);
        }

        protected override object GetIndentedSettings()
        {
            return new JsonSerializerSettings { Formatting = Formatting.Indented };
        }

        private static IFileBufferingStreamFactory GetFileBufferingStreamFactory()
        {
            return new HttpFileBufferingStreamFactory(Options.Create(new HttpBufferingOptions
            {
                TempFileDirectory = Path.GetTempPath()
            }));
        }
    }
}
