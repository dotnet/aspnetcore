// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class EmptyWebTemplateTest : PuppeteerTestsBase
    {
        public EmptyWebTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task EmptyWebTemplate_Works()
        {
            await TemplateBase("web", httpPort: 6040, httpsPort: 6041);
        }
    }
}
