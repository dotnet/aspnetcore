// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class WebHostTests
    {
        [Fact]
        public void WebHostConfiguration_IncludesCommandLineArguments()
        {
            var builder = WebHost.CreateDefaultBuilder(new string[] { "--urls", "http://localhost:5001" });
            Assert.Equal("http://localhost:5001", builder.GetSetting(WebHostDefaults.ServerUrlsKey));
        }
    }
}
