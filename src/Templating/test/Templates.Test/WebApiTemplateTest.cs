// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class WebApiTemplateTest : PuppeteerTestsBase
    {
        public WebApiTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task WebApiTemplate_Works()
        {
            await TemplateBase("webapi", targetFrameworkOverride: null, httpPort: 6050, httpsPort: 6051);
        }
    }
}
