using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocketsSample.Hubs
{
    public class Chat : Hub
    {
        public void Send(string message)
        {
            Clients.All.Invoke("Send", message);
        }

        public Person EchoPerson(Person p)
        {
            return p;
        }
    }
}
