using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.ConfigurationModel;

namespace Kestrel
{
    public class ServerInformation : IServerInformation
    {
        public void Initialize(IConfiguration configuration)
        {
        }

        public string Name
        {
            get
            {
                return "Kestrel";
            }
        }
    }
}