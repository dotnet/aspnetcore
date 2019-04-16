using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubClientBase : HubClientsBase<IClientProxy>
    {
        public override IClientProxy All => throw new NotImplementedException();

        public override IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            throw new NotImplementedException();
        }

        public override IClientProxy Client(string connectionId)
        {
            throw new NotImplementedException();
        }

        public override IClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            throw new NotImplementedException();
        }

        public override IClientProxy Group(string groupName)
        {
            throw new NotImplementedException();
        }

        public override IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            throw new NotImplementedException();
        }

        public override IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            throw new NotImplementedException();
        }

        public override IClientProxy User(string userId)
        {
            throw new NotImplementedException();
        }

        public override IClientProxy Users(IReadOnlyList<string> userIds)
        {
            throw new NotImplementedException();
        }
    }
}
