// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class WebApiTemplateTest : PuppeteerTestsBase
    {
        public WebApiTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task WebApiTemplate_Works_NetFramework()
            => await WebApiTemplateImpl("net461");

        [Fact]
        public async Task WebApiTemplate_Works_NetCore()
            => await WebApiTemplateImpl(null);

        private async Task WebApiTemplateImpl(string targetFrameworkOverride)
        {
            await TemplateBase("webapi", targetFrameworkOverride, httpPort: 6050, httpsPort: 6051);
        }
    }
}
