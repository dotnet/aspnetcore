using System.Collections.Generic;

namespace Microsoft.AspNet.PipelineCore.Owin
{
    public interface ICanHasOwinEnvironment
    {
        IDictionary<string, object> Environment { get; set; }
    }
}