// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCoreSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configure
            var services = new ServiceCollection()
                .AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddDbContext<DataProtectionKeyContext>(o =>
                {
                    o.UseInMemoryDatabase("DataProtection_EntityFrameworkCore");
                    o.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                    o.EnableSensitiveDataLogging();
                })
                .AddDataProtection()
                .PersistKeysToDbContext<DataProtectionKeyContext>()
                .SetDefaultKeyLifetime(TimeSpan.FromDays(7))
                .Services
                .BuildServiceProvider(validateScopes: true);

            using(services)
            {
                // Run a sample payload
                var protector = services.GetDataProtector("sample-purpose");
                var protectedData = protector.Protect("Hello world!");
                Console.WriteLine(protectedData);
            }
        }
    }

    class DataProtectionKeyContext : DbContext, IDataProtectionKeyContext
    {
        public DataProtectionKeyContext(DbContextOptions<DataProtectionKeyContext> options) : base(options) { }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    }
}
