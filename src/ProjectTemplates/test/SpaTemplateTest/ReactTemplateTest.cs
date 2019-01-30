// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Templates.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

[assembly: AssemblyFixture(typeof(SeleniumServerFixture))]
namespace Templates.Test.SpaTemplateTest
{
    public class ReactTemplateTest : SpaTemplateTestBase
    {
        public ReactTemplateTest(BrowserFixture browserFixture, ITestOutputHelper output) : base(browserFixture, output)
        {
        }

        [Fact]
        public void ReactTemplate_Works_NetCore()
            => SpaTemplateImpl("react");
    }
}
