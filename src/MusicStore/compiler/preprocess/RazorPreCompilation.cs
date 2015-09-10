using System;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Dnx.Runtime;

namespace MusicStore
{
    public class RazorPreCompilation : RazorPreCompileModule
    {
        public RazorPreCompilation(IApplicationEnvironment applicationEnvironment)
        {
            GenerateSymbols = string.Equals(applicationEnvironment.Configuration,
                                            "debug",
                                            StringComparison.OrdinalIgnoreCase);
        }
    }
}