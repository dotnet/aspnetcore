// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TempDataInSessionTest : TempDataTestBase, IClassFixture<MvcTestFixture<BasicWebSite.StartupWithSessionTempDataProvider>>
    {
        public TempDataInSessionTest(MvcTestFixture<BasicWebSite.StartupWithSessionTempDataProvider> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        protected override HttpClient Client { get; }
    }
}