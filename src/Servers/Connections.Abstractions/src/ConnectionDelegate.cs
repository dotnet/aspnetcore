using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    public delegate Task ConnectionDelegate(ConnectionContext connection);
}
