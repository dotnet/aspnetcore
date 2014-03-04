using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate a role
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRoleValidator<in T>
    {
        /// <summary>
        ///     Validate the user
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        Task<IdentityResult> Validate(T role);
    }
}