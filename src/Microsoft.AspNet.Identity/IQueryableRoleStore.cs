using System.Linq;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes an IQueryable roles
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public interface IQueryableRoleStore<TRole> : IQueryableRoleStore<TRole, string> where TRole : IRole<string>
    {
    }

    /// <summary>
    ///     Interface that exposes an IQueryable roles
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IQueryableRoleStore<TRole, in TKey> : IRoleStore<TRole, TKey> where TRole : IRole<TKey>
    {
        /// <summary>
        ///     IQueryable users
        /// </summary>
        IQueryable<TRole> Roles { get; }
    }
}