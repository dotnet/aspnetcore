using System;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Dnx.Runtime;

namespace MusicStore
{
    public class RazorPreCompilation : RazorPreCompileModule
    {
        public RazorPreCompilation(IServiceProvider provider,
                                   IApplicationEnvironment applicationEnvironment)
            : base(provider)
        {
            GenerateSymbols = string.Equals(applicationEnvironment.Configuration,
                                            "debug",
                                            StringComparison.OrdinalIgnoreCase);
        }
    }
}