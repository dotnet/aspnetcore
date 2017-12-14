// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
#if NET461
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public abstract class MSBuildIntegrationTestBase
    {
#if NET461
#elif NETCOREAPP2_0 || NETCOREAPP2_1
        private static readonly AsyncLocal<ProjectDirectory> _project = new AsyncLocal<ProjectDirectory>();
#else
#error TFM not supported
#endif

        protected MSBuildIntegrationTestBase()
        {
        }

        // Used by the test framework to set the project that we're working with
        internal static ProjectDirectory Project
        {
#if NET461
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("MSBuildIntegrationTestBase_Project");
                return (ProjectDirectory)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("MSBuildIntegrationTestBase_Project", new ObjectHandle(value));
            }
#elif NETCOREAPP2_0 || NETCOREAPP2_1
            get { return _project.Value; }
            set { _project.Value = value; }
#else
#error TFM not supported
#endif
        }

        internal Task<MSBuildResult> DotnetMSBuild(string target, string args = null, bool debug = false)
        {
            var timeout = debug ? (TimeSpan?)Timeout.InfiniteTimeSpan : null;
            return MSBuildProcessManager.RunProcessAsync(Project, $"/t:{target} {args}", timeout);
        }
    }
}
