using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.InMemory
{
    public class InMemoryRoleStore : IQueryableRoleStore<InMemoryRole, string>
    {
        private readonly Dictionary<string, InMemoryRole> _roles = new Dictionary<string, InMemoryRole>();

        public Task Create(InMemoryRole role)
        {
            _roles[role.Id] = role;
            return Task.FromResult(0);
        }

        public Task Delete(InMemoryRole role)
        {
            if (role == null || !_roles.ContainsKey(role.Id))
            {
                throw new InvalidOperationException("Unknown role");
            }
            _roles.Remove(role.Id);
            return Task.FromResult(0);
        }

        public Task Update(InMemoryRole role)
        {
            _roles[role.Id] = role;
            return Task.FromResult(0);
        }

        public Task<InMemoryRole> FindById(string roleId)
        {
            if (_roles.ContainsKey(roleId))
            {
                return Task.FromResult(_roles[roleId]);
            }
            return Task.FromResult<InMemoryRole>(null);
        }

        public Task<InMemoryRole> FindByName(string roleName)
        {
            return
                Task.FromResult(
                    Roles.SingleOrDefault(r => String.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase)));
        }

        public void Dispose()
        {
        }

        public IQueryable<InMemoryRole> Roles
        {
            get { return _roles.Values.AsQueryable(); }
        }
    }
}