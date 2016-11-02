using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IGroupManager
    {
        Task Add(string groupName);
        Task Remove(string groupName);
    }
}
