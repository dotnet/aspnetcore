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

        [Theory]
        [InlineData(null)]
        [InlineData("F#", Skip = "https://github.com/aspnet/Templating/issues/673")]
        private async Task MvcTemplate_NoAuthImpl(string languageOverride)
            => await MvcTemplateBase(targetFrameworkOverride: null, languageOverride: languageOverride);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MvcTemplate_IndividualAuth(bool useLocalDb)
            => await MvcTemplateBase(targetFrameworkOverride: null, auth: IndividualAuth, useLocalDb: useLocalDb);

        private async Task MvcTemplateBase(
            string targetFrameworkOverride,
            string languageOverride = null,
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
