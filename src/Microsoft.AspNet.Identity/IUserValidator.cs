using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate a user
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IUserValidator<in T>
    {
        /// <summary>
        ///     Validate the user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<IdentityResult> Validate(T user);
    }
}