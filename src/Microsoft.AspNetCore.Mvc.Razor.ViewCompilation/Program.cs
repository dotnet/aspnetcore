// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    internal class Program
    {
        private readonly static Type ProgramType = typeof(Program);

        public static int Main(string[] args)
        {
#if DEBUG
            DebugHelper.HandleDebugSwitch(ref args);
#endif

            var app = new PrecompilationApplication(ProgramType);
            new PrecompileRunCommand().Configure(app);
            return app.Execute(args);
        }
    }
}
