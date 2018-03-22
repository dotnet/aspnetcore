using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    /// <summary>
    /// <see cref="ApplicationPartFactory"/> for Microsoft.AspNetCore.Authentication.AzureADB2C.UI.Views.dll
    /// </summary>
    public abstract class AzureADB2CPartFactory : ApplicationPartFactory
    {
        /// <inheritdoc />
        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly, string context) => 
            Array.Empty<ApplicationPart>();

        /// <summary>
        /// Creates the list of <see cref="ApplicationPart"/> for a given application.
        /// </summary>
        /// <returns>The <see cref="ApplicationPart"/> list.</returns>
        public abstract IEnumerable<ApplicationPart> CreateApplicationParts();
    }
}
