using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public interface IServerDelegationFeature
    {
        /// <summary>
        /// Create a delegation rule on request queue owned by the server.
        /// </summary>
        /// <returns>Creates a <see cref="DelegationRule"/> can used to transfer individual requests</returns>
        DelegationRule CreateDelegationRule(string queueName, string urlPrefix);
    }
}
