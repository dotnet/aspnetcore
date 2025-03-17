// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class KestrelBasedWapFactory : WebApplicationFactory<SimpleWebSite.Startup>
{
    public KestrelBasedWapFactory() : base()
    {
        // Use dynamically assigned port to avoid test conflicts in CI.
        this.UseKestrel(0);
    }
}
