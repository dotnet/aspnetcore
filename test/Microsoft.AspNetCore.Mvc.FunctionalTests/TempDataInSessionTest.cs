// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TempDataInSessionTest : TempDataTestBase, IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public TempDataInSessionTest(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        protected override HttpClient Client { get; }
    }
}