using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Protocols
{
    public delegate Task ConnectionDelegate(ConnectionContext connection);
}
