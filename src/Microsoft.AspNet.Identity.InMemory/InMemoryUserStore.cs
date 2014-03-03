using System;
using System.Collections.Generic;
using System.Linq;
#if NET45
using System.Security.Claims;
#else
using System.Security.ClaimsK;
#endif
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.InMemory
{
    public class InMemoryUserStore : 
        IUserStore<InMemoryUser, string>, 
        IUserLoginStore<InMemoryUser, string>, 
        IUserRoleStore<InMemoryUser, string>,
        IUserClaimStore<InMemoryUser, string>, 
        IUserPasswordStore<InMemoryUser, string>, 
        IUserSecurityStampStore<InMemoryUser, string>,
        IUserEmailStore<InMemoryUser, string>,
        IUserLockoutStore<InMemoryUser, string>,
        IUserPhoneNumberStore<InMemoryUser, string>
    {
        private readonly Dictionary<UserLoginInfo, InMemoryUser> _logins =
            new Dictionary<UserLoginInfo, InMemoryUser>(new LoginComparer());

        private readonly Dictionary<string, InMemoryUser> _users = new Dictionary<string, InMemoryUser>();

        public IQueryable<InMemoryUser> Users
        {
            get { return _users.Values.AsQueryable(); }
        }

        public Task<IList<Claim>> GetClaims(InMemoryUser user)
        {
            return Task.FromResult(user.Claims);
        }

        public Task AddClaim(InMemoryUser user, Claim claim)
        {
            user.Claims.Add(claim);
            return Task.FromResult(0);
        }

        public Task RemoveClaim(InMemoryUser user, Claim claim)
        {
            user.Claims.Remove(claim);
            return Task.FromResult(0);
        }

        public Task AddLogin(InMemoryUser user, UserLoginInfo login)
        {
            user.Logins.Add(login);
            _logins[login] = user;
            return Task.FromResult(0);
        }

        public Task RemoveLogin(InMemoryUser user, UserLoginInfo login)
        {
            var logs =
                user.Logins.Where(l => l.ProviderKey == login.ProviderKey && l.LoginProvider == login.LoginProvider);
            foreach (var l in logs)
            {
                user.Logins.Remove(l);
                _logins[l] = null;
            }
            return Task.FromResult(0);
        }

        public Task<IList<UserLoginInfo>> GetLogins(InMemoryUser user)
        {
            return Task.FromResult(user.Logins);
        }

        public Task<InMemoryUser> Find(UserLoginInfo login)
        {
            if (_logins.ContainsKey(login))
            {
                return Task.FromResult(_logins[login]);
            }
            return Task.FromResult<InMemoryUser>(null);
        }

        public Task SetPasswordHash(InMemoryUser user, string passwordHash)
        {
            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHash(InMemoryUser user)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPassword(InMemoryUser user)
        {
            return Task.FromResult(user.PasswordHash != null);
        }

        public Task AddToRole(InMemoryUser user, string role)
        {
            user.Roles.Add(role);
            return Task.FromResult(0);
        }

        public Task RemoveFromRole(InMemoryUser user, string role)
        {
            user.Roles.Remove(role);
            return Task.FromResult(0);
        }

        public Task<IList<string>> GetRoles(InMemoryUser user)
        {
            return Task.FromResult(user.Roles);
        }

        public Task<bool> IsInRole(InMemoryUser user, string role)
        {
            return Task.FromResult(user.Roles.Contains(role));
        }

        public Task SetSecurityStamp(InMemoryUser user, string stamp)
        {
            user.SecurityStamp = stamp;
            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStamp(InMemoryUser user)
        {
            return Task.FromResult(user.SecurityStamp);
        }

        public Task Create(InMemoryUser user)
        {
            _users[user.Id] = user;
            return Task.FromResult(0);
        }

        public Task Update(InMemoryUser user)
        {
            _users[user.Id] = user;
            return Task.FromResult(0);
        }

        public Task<InMemoryUser> FindById(string userId)
        {
            if (_users.ContainsKey(userId))
            {
                return Task.FromResult(_users[userId]);
            }
            return Task.FromResult<InMemoryUser>(null);
        }

        public void Dispose()
        {
        }

        public Task<InMemoryUser> FindByName(string userName)
        {
            return Task.FromResult(Users.FirstOrDefault(u => String.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase)));
        }

        public Task Delete(InMemoryUser user)
        {
            if (user == null || !_users.ContainsKey(user.Id))
            {
                throw new InvalidOperationException("Unknown user");
            }
            _users.Remove(user.Id);
            return Task.FromResult(0);
        }

        public Task SetEmail(InMemoryUser user, string email)
        {
            user.Email = email;
            return Task.FromResult(0);
        }

        public Task<string> GetEmail(InMemoryUser user)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmed(InMemoryUser user)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmed(InMemoryUser user, bool confirmed)
        {
            user.EmailConfirmed = confirmed;
            return Task.FromResult(0);
        }

        public Task<InMemoryUser> FindByEmail(string email)
        {
            return Task.FromResult(Users.FirstOrDefault(u => String.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<DateTimeOffset> GetLockoutEndDate(InMemoryUser user)
        {
            return Task.FromResult(user.LockoutEnd);
        }

        public Task SetLockoutEndDate(InMemoryUser user, DateTimeOffset lockoutEnd)
        {
            user.LockoutEnd = lockoutEnd;
            return Task.FromResult(0);
        }

        public Task<int> IncrementAccessFailedCount(InMemoryUser user)
        {
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCount(InMemoryUser user)
        {
            user.AccessFailedCount = 0;
            return Task.FromResult(0);
        }

        public Task<int> GetAccessFailedCount(InMemoryUser user)
        {
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabled(InMemoryUser user)
        {
            return Task.FromResult(user.LockoutEnabled);
        }

        public Task SetLockoutEnabled(InMemoryUser user, bool enabled)
        {
            user.LockoutEnabled = enabled;
            return Task.FromResult(0);
        }

        public Task SetPhoneNumber(InMemoryUser user, string phoneNumber)
        {
            user.PhoneNumber = phoneNumber;
            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumber(InMemoryUser user)
        {
            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmed(InMemoryUser user)
        {
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task SetPhoneNumberConfirmed(InMemoryUser user, bool confirmed)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.FromResult(0);
        }

            private class LoginComparer : IEqualityComparer<UserLoginInfo>
            {
                public bool Equals(UserLoginInfo x, UserLoginInfo y)
                {
                    return x.LoginProvider == y.LoginProvider && x.ProviderKey == y.ProviderKey;
                }

                public int GetHashCode(UserLoginInfo obj)
                {
                    return (obj.ProviderKey + "--" + obj.LoginProvider).GetHashCode();
                }
            }

    }
}