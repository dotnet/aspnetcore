using Microsoft.AspNet.Identity.Entity;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddEntity<TUser, TRole>(this IdentityBuilder<TUser, TRole> builder)
            where TUser : EntityUser
            where TRole : EntityRole
        {
            builder.Services.AddScoped<IUserStore<TUser>, UserStore<TUser>>();
            builder.Services.AddScoped<UserManager<TUser>, UserManager<TUser>>();
            builder.Services.AddScoped<IRoleStore<TRole>, RoleStore<TRole>>();
            builder.Services.AddScoped<RoleManager<TRole>, RoleManager<TRole>>();
            return builder;
        }
    }
}