namespace SocketsSample.Hubs
{
    public interface IGroupManager
    {
        void Add(string groupName);
        void Remove(string groupName);
    }
}
