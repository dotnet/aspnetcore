using System.Collections.Generic;

namespace Microsoft.AspNet.Owin
{
    public interface ICanHasOwinEnvironment
    {
        IDictionary<string, object> Environment { get; set; }
    }
}