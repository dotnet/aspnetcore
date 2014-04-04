using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate passwords
    /// </summary>
    public interface IPasswordValidator
    {
        /// <summary>
        ///     ValidateAsync the item
        /// </summary>
        /// <returns></returns>
        Task<IdentityResult> ValidateAsync(string password, CancellationToken cancellationToken = default(CancellationToken));
    }
}