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
        /// <returns></returns>
        Task Send(IdentityMessage message);
    }
}