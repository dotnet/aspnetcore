using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature.Security
{
    [AssemblyNeutral]
    public interface IChallengeContext
    {
        IList<string> AuthenticationTypes {get;}
        IDictionary<string,string> Properties {get;}

        void Accept(string authenticationType, IDictionary<string,object> description);
    }
}