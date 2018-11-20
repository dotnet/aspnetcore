// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class EmptyWebTemplateTest : PuppeteerTestsBase
    {
        public EmptyWebTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task EmptyWebTemplate_Works_NetFramework()
            => await EmptyWebTemplateImpl("net461");

        [Fact]
        public async Task EmptyWebTemplate_Works_NetCore()
            => await EmptyWebTemplateImpl(null);

        private async Task EmptyWebTemplateImpl(string targetFrameworkOverride)
        {
            await TemplateBase("web", targetFrameworkOverride, httpPort: 6040, httpsPort: 6041);
        }
    }
}
