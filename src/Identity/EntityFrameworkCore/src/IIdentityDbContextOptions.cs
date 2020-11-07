using System;

using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore
{
    /// <summary>
    /// Identity stores database context configuration.
    /// </summary>
    public interface IIdentityDbContextOptions
    {
        /// <summary>
        /// Identity stores the database context type.
        /// </summary>
        Type DbContextType { get; }
    }

    /// <summary>
    /// Identity store database context configuration default implementation.
    /// </summary>
    public class IdentityDbContextOptions<TDbContext> : IIdentityDbContextOptions
        where TDbContext : DbContext
    {
        /// <summary>
        /// Identity stores the database context type.
        /// </summary>
        public virtual Type DbContextType { get; }

        /// <summary>
        /// constructor.
        /// </summary>
        public IdentityDbContextOptions()
        {
            this.DbContextType = typeof(TDbContext);
        }
    }
}
