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

    /// <summary>
    ///     Represents a message
    /// </summary>
    public class IdentityMessage
    {
        /// <summary>
        ///     Destination, i.e. To email, or SMS phone number
        /// </summary>
        public virtual string Destination { get; set; }

        /// <summary>
        ///     Subject
        /// </summary>
        public virtual string Subject { get; set; }

        /// <summary>
        ///     Message contents
        /// </summary>
        public virtual string Body { get; set; }
    }
}