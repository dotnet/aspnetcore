// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class RazorPagesTests : PuppeteerTestsBase
    {
        public RazorPagesTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task RazorPagesTemplate_NoAuth_Works_NetFramework()
            => await RazorPagesBase("net461");

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task RazorPagesTemplate_NoAuth_NoHttps_Works_NetFramework()
            => await RazorPagesBase("net461", noHttps: true);

        [Fact]
        public async Task RazorPagesTemplate_NoAuth_Works_NetCore()
            => await RazorPagesBase(targetFrameworkOverride: null);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task RazorPagesTemplate_IndividualAuth_Works_NetFramework()
            => await RazorPagesBase("net461", auth: IndividualAuth);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task RazorPagesTemplate_WithIndividualAuth_NoHttpsSetToTrue_UsesHttps_NetFramework()
            => await RazorPagesBase("net461", auth: IndividualAuth, useLocalDb: false, noHttps: true);

        [Fact]
        public async Task RazorPagesTemplate_IndividualAuth_Works_NetCore()
            => await RazorPagesBase(targetFrameworkOverride: null, auth: IndividualAuth);

        [Fact]
        public async Task RazorPagesTemplate_IndividualAuth_UsingLocalDB_Works_NetCore()
            => await RazorPagesBase(targetFrameworkOverride: null, auth: IndividualAuth, useLocalDb: true);

        private async Task RazorPagesBase(
            string targetFrameworkOverride,
            string languageOverride = default,
            string auth = null,
            bool noHttps = false,
            bool useLocalDb = false)
        => await TemplateBase("razor", targetFrameworkOverride, httpPort: 5100, httpsPort: 5101, languageOverride, auth, noHttps, useLocalDb);
    }
}
