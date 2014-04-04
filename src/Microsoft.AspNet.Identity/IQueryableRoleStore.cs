using System.Linq;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes an IQueryable roles
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public interface IQueryableRoleStore<TRole> : IRoleStore<TRole> where TRole : class
    {
        /// <summary>
        ///     IQueryable users
        /// </summary>
        IQueryable<TRole> Roles { get; }
    }
}