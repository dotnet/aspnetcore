using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate passwords
    /// </summary>
    public interface IPasswordValidator
    {
        /// <summary>
        ///     Validate the item
        /// </summary>
        /// <returns></returns>
        Task<IdentityResult> Validate(string password);
    }
}