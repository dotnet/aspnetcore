// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    /// <summary>
    /// A fixture that relies on the build task to spin up the build server.
    /// </summary>
    public class LegacyBuildServerTestFixture : BuildServerTestFixtureBase
    {
        public LegacyBuildServerTestFixture()
            : base(Guid.NewGuid().ToString())
        {
        }
    }
}
