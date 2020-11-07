using System;
using System.Reflection;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore
{
    /// <summary>
    /// Identity stores the database context provider.
    /// </summary>
    public interface IIdentityDbContextProvider
    {
        /// <summary>
        /// Gets the database context of identity store.
        /// </summary>
        /// <returns></returns>
        DbContext GetDbContext();
    }

    /// <summary>
    /// Identity stores the database context provider default implementation.
    /// </summary>
    public class IdentityDbContextProvider : IIdentityDbContextProvider, IDisposable
    {
        /// <summary>
        /// The scope container
        /// </summary>
        protected readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Identity stores database context configuration.
        /// </summary>
        protected readonly IIdentityDbContextOptions _options;
        /// <summary>
        /// Lazy loading identity stores the database context.
        /// </summary>
        protected readonly Lazy<DbContext> _instance;

        /// <inheritdoc/>
        public IdentityDbContextProvider(IServiceProvider serviceProvider, IIdentityDbContextOptions options)
        {
            this._serviceProvider = serviceProvider;
            this._options = options;

            this._instance = new Lazy<DbContext>(() =>
            {
                return (DbContext)this._serviceProvider.GetRequiredService(this._options.DbContextType);
            });
        }

        /// <inheritdoc/>
        public virtual DbContext GetDbContext()
        {
            return this._instance.Value;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (this._instance.IsValueCreated)
            {
                this._instance.Value?.Dispose();
            }
        }
    }

    /// <summary>
    /// The simplest identity store database context provider
    /// </summary>
    public class SampleDbContextProvider : IIdentityDbContextProvider
    {
        readonly DbContext _dbContext;

        /// <inheritdoc/>
        public SampleDbContextProvider(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc/>
        public DbContext GetDbContext()
        {
            return _dbContext;
        }
    }
}
