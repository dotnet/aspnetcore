// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.TestHost
{
    public class TestApplicationEnvironment : IApplicationEnvironment
    {
        public string ApplicationName
        {
            get { return "Test App environment"; }
        }

        public string Version
        {
            get { return "1.0.0"; }
        }

        public string ApplicationBasePath
        {
            get { return Environment.CurrentDirectory; }
        }

        public string Configuration
        {
            get { return "Test"; }
        }
        
        public FrameworkName RuntimeFramework
        {
            get { return new FrameworkName(".NETFramework", new Version(4, 5)); }
        }
    }
}
