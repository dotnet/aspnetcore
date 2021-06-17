// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class SystemTextJsonResultExecutorTest : SystemTextJsonResultExecutorTestBase
    {
        protected override IActionResultExecutor<JsonResult> CreateExecutor(ILoggerFactory loggerFactory)
        {
            return new SystemTextJsonResultExecutor(
                Options.Create(new JsonOptions()), 
                loggerFactory.CreateLogger<SystemTextJsonResultExecutor>(),
                Options.Create(new MvcOptions()));
        }
    }
}
