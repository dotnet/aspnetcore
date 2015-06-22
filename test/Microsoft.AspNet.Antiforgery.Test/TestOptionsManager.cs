// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Antiforgery
{
    public class TestOptionsManager : IOptions<AntiforgeryOptions>
    {
        public TestOptionsManager()
        {
        }

        public TestOptionsManager(AntiforgeryOptions options)
        {
            Options = options;
        }

        public AntiforgeryOptions Options { get; set; } = new AntiforgeryOptions();

        public AntiforgeryOptions GetNamedOptions(string name)
        {
            throw new NotImplementedException();
        }
    }
}
