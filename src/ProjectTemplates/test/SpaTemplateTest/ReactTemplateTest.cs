// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class ReactTemplateTest : SpaTemplateTestBase
    {
        private int _httpPort = 6020;
        private int _httpsPort = 6021;

        public ReactTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task ReactTemplate_Works()
            => await SpaTemplateImpl("react", _httpPort, _httpsPort);
    }
}
