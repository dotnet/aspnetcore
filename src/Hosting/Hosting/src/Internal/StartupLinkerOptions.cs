using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal static class StartupLinkerOptions
    {
        // We're going to keep all public constructors and public methods on Startup classes
        public const DynamicallyAccessedMemberTypes Accessibility = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods;
    }
}
