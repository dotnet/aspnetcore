using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Expose a way to send messages (email/txt)
    /// </summary>
    public interface IIdentityMessageService
    {
        /// <summary>
        ///     This method should send the message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendAsync(IdentityMessage message, CancellationToken cancellationToken = default(CancellationToken));
    }
}