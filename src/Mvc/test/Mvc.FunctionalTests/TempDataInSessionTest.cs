// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class TempDataInSessionTest : TempDataTestBase, IClassFixture<MvcTestFixture<BasicWebSite.StartupWithSessionTempDataProvider>>
{
    public TempDataInSessionTest(MvcTestFixture<BasicWebSite.StartupWithSessionTempDataProvider> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    protected override HttpClient Client { get; }
}
