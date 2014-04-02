using System.Collections.Generic;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IChallengeContext
    {
        IList<string> AuthenticationTypes {get;}
        IDictionary<string,string> Properties {get;}

        void Accept(string authenticationType, IDictionary<string,object> description);
    }
}