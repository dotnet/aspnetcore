using Microsoft.AspNet.DependencyInjection;
using System;

namespace Microsoft.AspNet.Identity
{
    public class IdentityBuilder<TUser, TRole> where TUser : class where TRole : class
    {
        private ServiceCollection Services { get; set; }

        public IdentityBuilder(ServiceCollection services)
        {
            Services = services;
        }

        public IdentityBuilder<TUser, TRole> Use<T>(Func<T> func)
        {
            Services.AddInstance<T>(func());
            return this;
        }

        public IdentityBuilder<TUser, TRole> UseIdentity()
        {
            Services.Add(IdentityServices.GetDefaultUserServices<TUser>());
            Services.Add(IdentityServices.GetDefaultRoleServices<TRole>());
            return this;
        }

        public IdentityBuilder<TUser, TRole> UseUserStore(Func<IUserStore<TUser>> func)
        {
            return Use(func);
        }

        public IdentityBuilder<TUser, TRole> UseRoleStore(Func<IRoleStore<TRole>> func)
        {
            return Use(func);
        }

        public IdentityBuilder<TUser, TRole> UsePasswordValidator(Func<IPasswordValidator> func)
        {
            return Use(func);
        }

        public IdentityBuilder<TUser, TRole> UseUserValidator(Func<IUserValidator<TUser>> func)
        {
            return Use(func);
        }

        public IdentityBuilder<TUser, TRole> UseUserManager<TManager>() where TManager : UserManager<TUser>
        {
            Services.AddScoped<TManager, TManager>();
            return this;
        }

        public IdentityBuilder<TUser, TRole> UseRoleManager<TManager>() where TManager : RoleManager<TRole>
        {
            Services.AddScoped<TManager, TManager>();
            return this;
        }

        //public IdentityBuilder<TUser, TRole> UseTwoFactorProviders(Func<IDictionary<string, IUserTokenProvider<TUser>>> func)
        //{
        //    return Use(func);
        //}

        public IdentityBuilder<TUser, TRole> UseLockoutPolicy(Func<LockoutPolicy> func)
        {
            return Use(func);
        }

    }
}