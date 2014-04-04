using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.InMemory
{
    public class InMemoryRoleStore<TRole> : IQueryableRoleStore<TRole> where TRole : InMemoryRole

    {
        private readonly Dictionary<string, TRole> _roles = new Dictionary<string, TRole>();

        public Task Create(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            _roles[role.Id] = role;
            return Task.FromResult(0);
        }

        public Task Delete(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null || !_roles.ContainsKey(role.Id))
            {
                throw new InvalidOperationException("Unknown role");
            }
            _roles.Remove(role.Id);
            return Task.FromResult(0);
        }

        public Task<string> GetRoleId(TRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(role.Id);
        }

        public Task<string> GetRoleName(TRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(role.Name);
        }

        public Task Update(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            _roles[role.Id] = role;
            return Task.FromResult(0);
        }

        public Task<TRole> FindById(string roleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_roles.ContainsKey(roleId))
            {
                return Task.FromResult(_roles[roleId]);
            }
            return Task.FromResult<TRole>(null);
        }

        public Task<TRole> FindByName(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return
                Task.FromResult(
                    Roles.SingleOrDefault(r => String.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase)));
        }

        public void Dispose()
        {
        }

        public IQueryable<TRole> Roles
        {
            get { return _roles.Values.AsQueryable(); }
        }
    }
}