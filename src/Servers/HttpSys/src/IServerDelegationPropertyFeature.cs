using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.HttpSys;

namespace Microsoft.AspNetCore.Hosting.Server.Features
{
    public interface IServerDelegationPropertyFeature
    {
        DelegationRule CreateDelegationRule(string queueName, string uri);
    }
}
