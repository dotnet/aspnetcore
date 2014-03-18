using System;

namespace Microsoft.AspNet.PipelineCore
{
    public interface ICanHasServiceProviders
    {
        IServiceProvider ApplicationServices { get; set; }
        IServiceProvider RequestServices { get; set; }
    }
}