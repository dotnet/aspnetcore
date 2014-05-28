// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity.Entity.Test
{
    public static class TestIdentityFactory
    {
        public static IdentityContext CreateContext()
        {
            var services = new ServiceCollection();
            
//#if NET45
//            services.AddEntityFramework().AddSqlServer();
//#else
            services.AddEntityFramework().AddInMemoryStore();
//#endif
            var serviceProvider = services.BuildServiceProvider();

            var db = new IdentityContext(serviceProvider);
             
            // TODO: Recreate DB, doesn't support String ID or Identity context yet
            db.Database.EnsureCreated();

            // TODO: CreateAsync DB?
            return db;
        }

        public class TestSetup : IOptionsSetup<IdentityOptions>
        {
            private readonly IdentityOptions _options;

            public TestSetup(IdentityOptions options)
            {
                _options = options;
            }

            public int Order { get { return 0; } }
            public void Setup(IdentityOptions options)
            {
                options.Copy(_options);
            }
        }


        public static UserManager<EntityUser> CreateManager(DbContext context)
        {
            var services = new ServiceCollection();
            services.AddTransient<IUserValidator<EntityUser>, UserValidator<EntityUser>>();
            services.AddTransient<IPasswordValidator<IdentityUser>, PasswordValidator<IdentityUser>>();
            services.AddInstance<IUserStore<EntityUser>>(new InMemoryUserStore<EntityUser>(context));
            services.AddSingleton<UserManager<EntityUser>, UserManager<EntityUser>>();
            var options = new IdentityOptions
            {
                Password = new PasswordOptions
                {
                    RequireDigit = false,
                    RequireLowercase = false,
                    RequireNonLetterOrDigit = false,
                    RequireUppercase = false
                }
            };
            var optionsAccessor = new OptionsAccessor<IdentityOptions>(new[] { new TestSetup(options) });
            //services.AddInstance<IOptionsAccessor<IdentityOptions>>(new OptionsAccessor<IdentityOptions>(new[] { new TestSetup(options) }));
            //return services.BuildServiceProvider().GetService<UserManager<EntityUser>>();
            return new UserManager<EntityUser>(services.BuildServiceProvider(), new InMemoryUserStore<EntityUser>(context), optionsAccessor);
        }

        public static UserManager<EntityUser> CreateManager()
        {
            return CreateManager(CreateContext());
        }

        public static RoleManager<EntityRole> CreateRoleManager(DbContext context)
        {
            var services = new ServiceCollection();
            services.AddTransient<IRoleValidator<EntityRole>, RoleValidator<EntityRole>>();
            services.AddInstance<IRoleStore<EntityRole>>(new EntityRoleStore<EntityRole, string>(context));
//            return services.BuildServiceProvider().GetService<RoleManager<EntityRole>>();
            return new RoleManager<EntityRole>(services.BuildServiceProvider(), new EntityRoleStore<EntityRole, string>(context));
        }

        public static RoleManager<EntityRole> CreateRoleManager()
        {
            return CreateRoleManager(CreateContext());
        }
    }
}
