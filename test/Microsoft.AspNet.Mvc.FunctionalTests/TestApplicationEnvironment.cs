// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // Represents an application environment that overrides the base path of the original
    // application environment in order to make it point to the folder of the original web
    // aplication so that components like ViewEngines can find views as if they were executing
    // in a regular context.
    public class TestApplicationEnvironment : IApplicationEnvironment
    {
        private readonly IApplicationEnvironment _originalAppEnvironment;
        private readonly string _applicationBasePath;

        public TestApplicationEnvironment(IApplicationEnvironment originalAppEnvironment, string appBasePath)
        {
            _originalAppEnvironment = originalAppEnvironment;
            _applicationBasePath = appBasePath;
        }

        public string ApplicationName
        {
            get { return _originalAppEnvironment.ApplicationName; }
        }

        public string Version
        {
            get { return _originalAppEnvironment.Version; }
        }

        public string ApplicationBasePath
        {
            get { return _applicationBasePath; }
        }

        public string Configuration
        {
            get
            {
                return _originalAppEnvironment.Configuration;
            }
        }

        public FrameworkName RuntimeFramework
        {
            get { return _originalAppEnvironment.RuntimeFramework; }
        }
    }
}