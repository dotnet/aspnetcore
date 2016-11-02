using System.Threading.Tasks;

namespace SocketsSample.Hubs
{
    public interface IClientProxy
    {
        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// </summary>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="args">argumetns to pass to the client</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        Task Invoke(string method, params object[] args);
    }
}
