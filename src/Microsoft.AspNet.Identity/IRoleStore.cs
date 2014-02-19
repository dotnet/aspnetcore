using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes basic role management
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRoleStore<TRole, in TKey> : IDisposable where TRole : IRole<TKey>
    {
        /// <summary>
        ///     Insert a new role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        Task Create(TRole role);

        /// <summary>
        ///     Update a role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        Task Update(TRole role);

        /// <summary>
        ///     Delete a role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        Task Delete(TRole role);

        /// <summary>
        ///     Finds a role by id
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        Task<TRole> FindById(TKey roleId);

        /// <summary>
        ///     Find a role by name
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<TRole> FindByName(string roleName);
    }
}