using System.Collections.Generic;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IAuthTypeContext
    {
        void Accept(IDictionary<string,object> description);
    }
}