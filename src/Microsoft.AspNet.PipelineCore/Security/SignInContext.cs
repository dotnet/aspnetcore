using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class SignInContext : ISignInContext
    {
        public SignInContext(ClaimsPrincipal user, IDictionary<string, string> dictionary)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            User = user;
            Properties = dictionary ?? new Dictionary<string, string>(StringComparer.Ordinal);
            Acked = new List<string>();
        }

        public ClaimsPrincipal User { get; private set; }

        public IDictionary<string, string> Properties { get; private set; }

        public IList<string> Acked { get; private set; }

        public void Ack(string authenticationType, IDictionary<string, object> description)
        {
            Acked.Add(authenticationType);
        }
    }
}
