// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class MvcTests : PuppeteerTestsBase
    {
        public MvcTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public async Task MvcTemplate_NoAuth_NoHttps_Works_NetCore_ForDefaultTemplate()
            => await MvcTemplateBase(targetFrameworkOverride: null, languageOverride: default, noHttps: true);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task MvcTemplate_NoAuth_Works_NetFramework_ForDefaultTemplate()
            => await MvcTemplateBase("net461", languageOverride: default);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task MvcTemplate_NoAuth_Works_NetFramework_ForFSharpTemplate()
            => await MvcTemplateBase("net461", languageOverride: "F#");

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task MvcTemplate_NoAuth_NoHttps_Works_NetFramework_ForDefaultTemplate()
            => await MvcTemplateBase("net461", languageOverride: default, noHttps: true);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task MvcTemplate_NoAuth_NoHttps_Works_NetFramework_ForFSharpTemplate()
            => await MvcTemplateBase("net461", languageOverride: "F#", noHttps: true);

        [Fact]
        public async Task MvcTemplate_NoAuth_Works_NetCore_ForDefaultTemplate()
            => await MvcTemplateBase(null, languageOverride: default);

        [Fact]
        public async Task MvcTemplate_NoAuth_Works_NetCore_ForFSharpTemplate()
            => await MvcTemplateBase(null, languageOverride: "F#");

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task MvcTemplate_IndividualAuth_Works_NetFramework()
            => await MvcTemplateBase("net461", auth: IndividualAuth);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task MvcTemplate_WithIndividualAuth_NoHttpsSetToTrue_UsesHttps_NetFramework()
            => await MvcTemplateBase("net461", auth: IndividualAuth, useLocalDb: false, noHttps: true);

        [Fact]
        public async Task MvcTemplate_IndividualAuth_Works_NetCore()
            => await MvcTemplateBase(targetFrameworkOverride: null, auth: IndividualAuth);

        [Fact]
        public async Task MvcTemplate_IndividualAuth_UsingLocalDB_Works_NetCore()
            => await MvcTemplateBase(targetFrameworkOverride: null, auth: IndividualAuth, useLocalDb: true);

        private async Task MvcTemplateBase(
            string targetFrameworkOverride,
            string languageOverride = default,
            string auth = null,
            bool noHttps = false,
            bool useLocalDb = false)
            => await TemplateBase(
                "mvc",
                targetFrameworkOverride,
                httpPort: 5002,
                httpsPort: 5003,
                languageOverride: languageOverride,
                auth,
                noHttps,
                useLocalDb);
    }
}
