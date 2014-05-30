using System;
using Microsoft.AspNet.Identity.Entity;
using Microsoft.Data.Entity;
using Microsoft.Framework.OptionsModel;

namespace MusicStore.Models
{
    public class ApplicationUser : User { }

    public class ApplicationDbContext : IdentitySqlContext<ApplicationUser>
    {
        private IdentityDbContextOptions options;

        public ApplicationDbContext(IServiceProvider serviceProvider, IOptionsAccessor<IdentityDbContextOptions> optionsAccessor)
                   : base(serviceProvider, optionsAccessor.Options)
        {
            options = optionsAccessor.Options;
        }

        protected override void OnConfiguring(DbContextOptions builder)
        {
            //Bug: Identity overriding the passed in connection string with a default value. https://github.com/aspnet/identity/issues/102
            builder.UseSqlServer(options.ConnectionString);
        }
    }

    public class IdentityDbContextOptions : DbContextOptions
    {
        public string DefaultAdminUserName { get; set; }

        public string DefaultAdminPassword { get; set; }

        //Bug: Identity overriding the passed in connection string with a default value. https://github.com/aspnet/identity/issues/102
        public string ConnectionString { get; set; }
    }
}