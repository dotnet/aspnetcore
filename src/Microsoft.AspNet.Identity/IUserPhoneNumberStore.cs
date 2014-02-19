using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores a user's phoneNumber
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IUserPhoneNumberStore<TUser, in TKey> : IUserStore<TUser, TKey> where TUser : class, IUser<TKey>
    {
        /// <summary>
        ///     Set the user PhoneNumber
        /// </summary>
        /// <param name="user"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        Task SetPhoneNumber(TUser user, string phoneNumber);

        /// <summary>
        ///     Get the user phoneNumber
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<string> GetPhoneNumber(TUser user);

        /// <summary>
        ///     Returns true if the user phone number is confirmed
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> GetPhoneNumberConfirmed(TUser user);

        /// <summary>
        ///     Sets whether the user phone number is confirmed
        /// </summary>
        /// <param name="user"></param>
        /// <param name="confirmed"></param>
        /// <returns></returns>
        Task SetPhoneNumberConfirmed(TUser user, bool confirmed);
    }
}