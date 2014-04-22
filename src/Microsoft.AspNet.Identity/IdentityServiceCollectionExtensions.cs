using Microsoft.AspNet.Identity;
using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static ServiceCollection AddIdentity<TUser, TRole>(this ServiceCollection services, Action<IdentityBuilder<TUser, TRole>> actionBuilder)
            where TUser : class
            where TRole : class
        {
            services.Add(IdentityServices.GetDefaultUserServices<TUser>());
            services.Add(IdentityServices.GetDefaultRoleServices<TRole>());
            actionBuilder(new IdentityBuilder<TUser, TRole>(services));
            return services;
        }

        public static ServiceCollection AddIdentity<TUser>(this ServiceCollection services, Action<IdentityBuilder<TUser, IdentityRole>> actionBuilder)
            where TUser : class
        {
            services.Add(IdentityServices.GetDefaultUserServices<TUser>());
            services.Add(IdentityServices.GetDefaultRoleServices<IdentityRole>());
            actionBuilder(new IdentityBuilder<TUser, IdentityRole>(services));
            return services;
        }
    }
}