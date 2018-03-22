using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    internal class AzureAdB2CViewsPartFactory : AzureADB2CPartFactory
    {
        public override IEnumerable<ApplicationPart> CreateApplicationParts()
        {
            yield return new CompiledRazorAssemblyPart(this.GetType().Assembly);
        }
    }
}
