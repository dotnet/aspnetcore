using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IGroupManager
    {
        Task AddAsync(string groupName);
        Task RemoveAsync(string groupName);
    }
}
