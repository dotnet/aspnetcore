// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for Program
    /// </summary>
    public class Program
    {
        private readonly IApplicationEnvironment env;
        private readonly IServiceProvider sp;
        private readonly ILibraryManager _libraryManager;
        private readonly IApplicationShutdown _shutdown;

        public Program(
            IApplicationEnvironment env,
            IServiceProvider sp,
            ILibraryManager libraryManager,
            IApplicationShutdown shutown)
        {
            this.env = env;
            this.sp = sp;
            _libraryManager = libraryManager;
            _shutdown = shutown;
        }

        public int Main()
        {
            return new Xunit.Runner.Dnx.Program(env, sp, _libraryManager, _shutdown).Main(new string[]
            {
                "-class",
                typeof(MultipleLoopTests).FullName
            });
        }
    }
}