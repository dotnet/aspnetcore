using System;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Runtime;

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