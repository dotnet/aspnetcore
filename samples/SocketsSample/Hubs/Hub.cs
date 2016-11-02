using System.Threading.Tasks;

namespace SocketsSample.Hubs
{
    public class Hub
    {
        public virtual Task OnConnectedAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDisconnectedAsync()
        {
            return Task.CompletedTask;
        }

        public IHubConnectionContext Clients { get; set; }

        public HubCallerContext Context { get; set; }

        public IGroupManager Groups { get; set; }
    }
}
