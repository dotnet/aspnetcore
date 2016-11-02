namespace Microsoft.AspNetCore.SignalR
{
    public interface IGroupManager
    {
        void Add(string groupName);
        void Remove(string groupName);
    }
}
