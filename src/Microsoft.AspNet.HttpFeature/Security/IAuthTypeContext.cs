using System.Collections.Generic;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IAuthTypeContext
    {
        void Ack(IDictionary<string,object> description);
    }
}