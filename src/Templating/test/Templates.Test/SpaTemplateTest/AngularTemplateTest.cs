// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class AngularTemplateTest : SpaTemplateTestBase
    {
        private int _httpPort = 6010;
        private int _httpsPort = 6011;

        public AngularTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task AngularTemplate_Works()
            => await SpaTemplateImpl(null, "angular", _httpPort, _httpsPort);
    }
}
