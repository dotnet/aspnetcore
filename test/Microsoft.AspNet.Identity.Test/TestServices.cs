using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Identity.Test
{
    public static class TestServices
    {
        public static IEnumerable<IServiceDescriptor> DefaultServices<TUser, TKey>()
            where TUser : class, IUser<TKey>
            where TKey : IEquatable<TKey>
        {
            var describer = new ServiceDescriber();
            yield return describer.Transient<IPasswordValidator, PasswordValidator>();
            yield return describer.Transient<IUserValidator<TUser, TKey>, UserValidator<TUser, TKey>>();
            yield return describer.Transient<IPasswordHasher, PasswordHasher>();
            yield return describer.Transient<IClaimsIdentityFactory<TUser, TKey>, ClaimsIdentityFactory<TUser, TKey>>();
            yield return describer.Transient<IUserStore<TUser, TKey>, NoopUserStore>();
        }

    }
}