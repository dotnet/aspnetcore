using Microsoft.AspNet.Testing;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.InMemory;
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
        public static EntityContext CreateContext()
        {
            var configuration = new EntityConfigurationBuilder()
                //.UseModel(model)
                            .UseDataStore(new InMemoryDataStore())
                            .BuildConfiguration();

            var db = new IdentityContext(configuration);
            //            var sql = db.Configuration.DataStore as SqlServerDataStore;
            //            if (sql != null)
            //            {
            //#if NET45
            //                var builder = new DbConnectionStringBuilder {ConnectionString = sql.ConnectionString};
            //                var targetDatabase = builder["Database"].ToString();

            //                // Connect to master, check if database exists, and create if not
            //                builder.Add("Database", "master");
            //                using (var masterConnection = new SqlConnection(builder.ConnectionString))
            //                {
            //                    masterConnection.Open();

            //                    var masterCommand = masterConnection.CreateCommand();
            //                    masterCommand.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE [name]=N'" + targetDatabase +
            //                                                "'";
            //                    if ((int?) masterCommand.ExecuteScalar() < 1)
            //                    {
            //                        masterCommand.CommandText = "CREATE DATABASE [" + targetDatabase + "]";
            //                        masterCommand.ExecuteNonQuery();

            //                        using (var conn = new SqlConnection(sql.ConnectionString))
            //                        {
            //                            conn.Open();
            //                            var command = conn.CreateCommand();
            //                            command.CommandText = @"
            //CREATE TABLE [dbo].[AspNetUsers] (
            //[Id]                   NVARCHAR (128) NOT NULL,
            //[Email]                NVARCHAR (256) NULL,
            //[EmailConfirmed]       BIT            NOT NULL,
            //[PasswordHash]         NVARCHAR (MAX) NULL,
            //[SecurityStamp]        NVARCHAR (MAX) NULL,
            //[PhoneNumber]          NVARCHAR (MAX) NULL,
            //[PhoneNumberConfirmed] BIT            NOT NULL,
            //[TwoFactorEnabled]     BIT            NOT NULL,
            //[LockoutEndDateUtc]    DATETIME       NULL,
            //[LockoutEnabled]       BIT            NOT NULL,
            //[AccessFailedCount]    INT            NOT NULL,
            //[UserName]             NVARCHAR (256) NOT NULL
            //) ";
            //                            //CONSTRAINT [PK_dbo.AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
            //                            command.ExecuteNonQuery();
            //                        }
            //                    }
            //                }
            //#else
            //                throw new NotSupportedException("SQL Server is not yet supported when running against K10.");
            //#endif
            //}


            // TODO: CreateAsync DB?
            return db;
        }


        public static UserManager<EntityUser> CreateManager(EntityContext context)
        {
            return new UserManager<EntityUser>(new UserStore(context));
        }

        public static UserManager<EntityUser> CreateManager()
        {
            return CreateManager(CreateContext());
        }

        public static RoleManager<EntityRole> CreateRoleManager(EntityContext context)
        {
            return new RoleManager<EntityRole>(new RoleStore<EntityRole, string>(context));
        }

        public static RoleManager<EntityRole> CreateRoleManager()
        {
            return CreateRoleManager(CreateContext());
        }
    }
}
