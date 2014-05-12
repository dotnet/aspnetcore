using System;
using Microsoft.AspNet.Identity.Entity;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;

namespace MusicStore.Models
{
    public class ApplicationUser : User { }

    public class ApplicationDbContext : IdentitySqlContext<ApplicationUser> 
    {
        private readonly IConfiguration _configuration;

        public ApplicationDbContext(IServiceProvider serviceProvider, IConfiguration configuration)
            : base(serviceProvider)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptions builder)
        {
            builder.UseSqlServer(_configuration.Get("Data:IdentityConnection:ConnectionString"));
        }
    }
}