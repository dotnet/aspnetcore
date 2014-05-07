using Microsoft.AspNet.Identity.Entity;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddEntity<TUser, TRole>(this IdentityBuilder<TUser, TRole> builder)
            where TUser : EntityUser
            where TRole : EntityRole
        {
            builder.Services.AddScoped<IUserStore<TUser>, InMemoryUserStore<TUser>>();
            builder.Services.AddScoped<UserManager<TUser>, UserManager<TUser>>();
            builder.Services.AddScoped<IRoleStore<TRole>, EntityRoleStore<TRole>>();
            builder.Services.AddScoped<RoleManager<TRole>, RoleManager<TRole>>();
            return builder;
        }

        public static IdentityBuilder<TUser, IdentityRole> AddEntity<TUser>(this IdentityBuilder<TUser, IdentityRole> builder)
            where TUser : User
        {
            builder.Services.AddScoped<IUserStore<TUser>, UserStore<TUser>>();
            builder.Services.AddScoped<UserManager<TUser>>();
            return builder;
        }
    }
}