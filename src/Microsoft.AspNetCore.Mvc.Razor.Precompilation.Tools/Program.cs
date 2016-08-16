// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor.Precompilation.Design.Internal;
using Microsoft.AspNetCore.Mvc.Razor.Precompilation.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation.Tools
{
    public class Program
    {
        public static int Main(string[] args)
        {
#if DEBUG
            DebugHelper.HandleDebugSwitch(ref args);
#endif

            var app = new PrecompilationApplication(typeof(Program));
            new PrecompileDispatchCommand().Configure(app);
            return app.Execute(args);
        }
    }
}
