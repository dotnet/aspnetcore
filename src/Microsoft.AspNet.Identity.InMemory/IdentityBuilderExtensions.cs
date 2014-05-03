using Microsoft.AspNet.Identity.InMemory;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddInMemory<TUser, TRole>(this IdentityBuilder<TUser, TRole> builder)
            where TUser : IdentityUser
            where TRole : IdentityRole
        {
            builder.Services.AddScoped<IUserStore<TUser>, InMemoryUserStore<TUser>>();
            builder.Services.AddScoped<UserManager<TUser>, UserManager<TUser>>();
            builder.Services.AddScoped<IRoleStore<TRole>, InMemoryRoleStore<TRole>>();
            builder.Services.AddScoped<RoleManager<TRole>, RoleManager<TRole>>();
            return builder;
        }
    }
}