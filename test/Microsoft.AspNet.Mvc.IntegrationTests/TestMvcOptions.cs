// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class TestMvcOptions : IOptions<MvcOptions>
    {
        public TestMvcOptions()
        {
            Options = new MvcOptions();
            MvcOptionsSetup.ConfigureMvc(Options);
        }

        public MvcOptions Options { get; }

        public MvcOptions GetNamedOptions(string name)
        {
            throw new NotImplementedException();
        }
    }
}