using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes basic role management
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public interface IRoleStore<TRole> : IDisposable where TRole : class
    {
        /// <summary>
        ///     Insert a new role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Create(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Update a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Update(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Delete a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Delete(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns a role's id
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetRoleId(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns a role's name
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetRoleName(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Finds a role by id
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TRole> FindById(string roleId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Find a role by name
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TRole> FindByName(string roleName, CancellationToken cancellationToken = default(CancellationToken));
    }
}