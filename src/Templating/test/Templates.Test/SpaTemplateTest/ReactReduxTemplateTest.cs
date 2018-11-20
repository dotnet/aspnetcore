// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class ReactReduxTemplateTest : SpaTemplateTestBase
    {
        private int _httpPort = 6030;
        private int _httpsPort = 6031;

        public ReactReduxTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task ReactReduxTemplate_Works_NetCore()
            => await SpaTemplateImpl(targetFrameworkOverride: null, "reactredux", _httpPort, _httpsPort);
    }
}
