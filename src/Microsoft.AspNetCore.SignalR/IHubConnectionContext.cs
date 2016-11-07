namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubConnectionContext<TClient>
    {
        TClient All { get; }

        TClient Client(string connectionId);

        TClient Group(string groupName);

        TClient User(string userId);
    }
}
