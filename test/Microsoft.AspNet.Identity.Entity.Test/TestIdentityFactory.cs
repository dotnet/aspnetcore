// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Testing;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.InMemory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Entity.Test
{
    public static class TestIdentityFactory
    {
        public static IdentityContext CreateContext()
        {
            var serviceProvider = new ServiceCollection()
//#if NET45
//                .AddEntityFramework(s => s.AddSqlServer())
//#else
                .AddEntityFramework(s => s.AddInMemoryStore())
//#endif
                .BuildServiceProvider();

            var db = new IdentityContext(serviceProvider);
             
            // TODO: Recreate DB, doesn't support String ID or Identity context yet
            if (!db.Database.Exists())
            {
                db.Database.Create();
            }

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
