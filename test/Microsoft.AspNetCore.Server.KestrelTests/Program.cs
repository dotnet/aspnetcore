// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    /// <summary>
    /// Summary description for Program
    /// </summary>
    public class Program
    {
        private readonly IApplicationEnvironment env;
        private readonly IServiceProvider sp;
        private readonly ILibraryManager _libraryManager;

        public Program(
            IApplicationEnvironment env,
            IServiceProvider sp,
            ILibraryManager libraryManager)
        {
            this.env = env;
            this.sp = sp;
            _libraryManager = libraryManager;
        }

        public int Main()
        {
            return Xunit.Runner.Dnx.Program.Main(new string[]
            {
                "-class",
                typeof(MultipleLoopTests).FullName
            });
        }
    }
}