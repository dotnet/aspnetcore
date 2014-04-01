using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions.Security;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class AuthTypeContext : IAuthTypeContext
    {
        public AuthTypeContext()
        {
            Results = new List<AuthenticationDescription>();
        }

        public IList<AuthenticationDescription> Results { get; private set; }
                
        public void Ack(IDictionary<string, object> description)
        {
            Results.Add(new AuthenticationDescription(description));
        }
    }
}
