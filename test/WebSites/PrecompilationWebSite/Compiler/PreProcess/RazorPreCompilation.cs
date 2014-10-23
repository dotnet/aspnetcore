// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Runtime;

namespace PrecompilationWebSite
{
    public class MyCompilation : RazorPreCompileModule
    {
        public MyCompilation(IServiceProvider provider) : base(ReplaceProvider(provider))
        {
        }

        // When running in memory tests the application base path will point to the functional tests
        // project folder.
        // We need to replace it to point to the actual precompilation website so that the views can
        // be found.
        public static IServiceProvider ReplaceProvider(IServiceProvider provider)
        {
            var originalEnvironment = provider.GetService<IApplicationEnvironment>();
            var newPath = Path.GetFullPath(
                Path.Combine(
                    originalEnvironment.ApplicationBasePath,
                    "..",
                    "WebSites",
                    "PrecompilationWebSite"));

            var precompilationApplicationEnvironment = new PrecompilationApplicationEnvironment(
                originalEnvironment,
                newPath);

            var collection = new ServiceCollection();
            collection.AddInstance<IApplicationEnvironment>(precompilationApplicationEnvironment);

            return collection.BuildServiceProvider(provider);
        }

        private class PrecompilationApplicationEnvironment : IApplicationEnvironment
        {
            private readonly IApplicationEnvironment _originalApplicationEnvironment;
            private readonly string _applicationBasePath;

            public PrecompilationApplicationEnvironment(IApplicationEnvironment original, string appBasePath)
            {
                _originalApplicationEnvironment = original;
                _applicationBasePath = appBasePath;
            }

            public string ApplicationName
            {
                get { return _originalApplicationEnvironment.ApplicationName; }
            }

            public string Version
            {
                get { return _originalApplicationEnvironment.Version; }
            }

            public string ApplicationBasePath
            {
                get { return _applicationBasePath; }
            }

            public string Configuration
            {
                get
                {
                    return _originalApplicationEnvironment.Configuration;
                }
            }

            public FrameworkName RuntimeFramework
            {
                get { return _originalApplicationEnvironment.RuntimeFramework; }
            }
        }
    }
}