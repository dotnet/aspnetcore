// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TestConfigurationProvider : ITestConfigurationProvider
    {
        public TestConfigurationProvider()
        {
            Configuration = new Configuration();
            Configuration.Add(new MemoryConfigurationSource());
        }

        public Configuration Configuration { get; set; }
    }
}