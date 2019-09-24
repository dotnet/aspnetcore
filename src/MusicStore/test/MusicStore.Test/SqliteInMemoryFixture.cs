using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicStore.Models;

namespace MusicStore
{
    public class SqliteInMemoryFixture : IDisposable
    {
        private IServiceScope _serviceScope;
        private SqliteConnection _connection;

        public virtual void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
            _serviceScope?.Dispose();
            _serviceScope= null;
        }

        public virtual IServiceCollection ConfigureServices(IServiceCollection services)
            => services
                .AddMemoryCache()
                .AddLogging()
                .AddDbContext<MusicStoreContext>(b => b.UseSqlite(_connection));

        public virtual IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceScope == null)
                {
                    _serviceScope = ConfigureServices(new ServiceCollection()).BuildServiceProvider().CreateScope();
                }

                return _serviceScope.ServiceProvider;
            }
        }

        public virtual MusicStoreContext Context
            => ServiceProvider.GetRequiredService<MusicStoreContext>();

        public virtual void CreateDatabase()
        {
            Dispose();
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            Context.Database.EnsureCreated();
        }
    }
}
