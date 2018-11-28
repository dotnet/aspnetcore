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

        [Fact]
        public async Task RazorPagesTemplate_NoAuth()
            => await RazorPagesBase(targetFrameworkOverride: null);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RazorPagesTemplate_IndividualAuthImpl(bool useLocalDB)
            => await RazorPagesBase(targetFrameworkOverride: null, auth: IndividualAuth, useLocalDb: useLocalDB);

        private async Task RazorPagesBase(
            string targetFrameworkOverride,
            string languageOverride = null,
            string auth = null,
            bool noHttps = false,
            bool useLocalDb = false)
        => await TemplateBase("razor", targetFrameworkOverride, httpPort: 5100, httpsPort: 5101, languageOverride, auth, noHttps, useLocalDb);
    }
}
