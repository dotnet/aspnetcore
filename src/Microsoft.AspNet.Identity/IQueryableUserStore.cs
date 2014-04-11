using System.Linq;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes an IQueryable users
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IQueryableUserStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     IQueryable users
        /// </summary>
        IQueryable<TUser> Users { get; }
    }
}