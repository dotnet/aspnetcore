using System;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultCanHasServiceProviders : ICanHasServiceProviders
    {
        public IServiceProvider ApplicationServices { get; set; }
        public IServiceProvider RequestServices { get; set; }
    }
}