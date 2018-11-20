// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        // Just use 'angular' as representative for .NET 4.6.1 coverage, as
        // the client-side code isn't affected by the .NET runtime choice
        public async Task AngularTemplate_Works_NetFramework()
            => await SpaTemplateImpl("net461", "angular", _httpPort, _httpsPort);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task AngularTemplate_NoHttps_Works_NetFramework()
            => await SpaTemplateImpl("net461", "angular", _httpPort, _httpsPort, true);

        [Fact]
        public async Task AngularTemplate_Works_NetCore()
            => await SpaTemplateImpl(null, "angular", _httpPort, _httpsPort);
    }
}
