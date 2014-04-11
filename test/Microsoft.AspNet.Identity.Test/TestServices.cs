using Microsoft.AspNet.DependencyInjection;
using System.Collections.Generic;

namespace Microsoft.AspNet.Identity.Test
{
    public static class TestServices
    {
        public static IEnumerable<IServiceDescriptor> DefaultServices<TUser, TKey>()
            where TUser : class
        {
            var describer = new ServiceDescriber();
            yield return describer.Transient<IPasswordValidator, PasswordValidator>();
            yield return describer.Transient<IUserValidator<TUser>, UserValidator<TUser>>();
            yield return describer.Transient<IPasswordHasher, PasswordHasher>();
            yield return describer.Transient<IClaimsIdentityFactory<TUser>, ClaimsIdentityFactory<TUser>>();
            yield return describer.Transient<IUserStore<TUser>, NoopUserStore>();
        }

    }
}