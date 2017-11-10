// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthSamples.FunctionalTests
{
    public class SampleTestFixture<TStartup> : WebApplicationTestFixture<TStartup> where TStartup : class
    {
        public SampleTestFixture() : base(Path.Combine("samples", typeof(TStartup).Assembly.GetName().Name)) { }
    }
}
