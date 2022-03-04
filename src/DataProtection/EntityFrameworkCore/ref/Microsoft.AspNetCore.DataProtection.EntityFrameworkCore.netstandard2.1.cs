// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    public static partial class EntityFrameworkCoreDataProtectionExtensions
    {
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToDbContext<TContext>(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder) where TContext : Microsoft.EntityFrameworkCore.DbContext, Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.IDataProtectionKeyContext { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
{
    public partial class DataProtectionKey
    {
        public DataProtectionKey() { }
        public string FriendlyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Xml { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class EntityFrameworkCoreXmlRepository<TContext> : Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository where TContext : Microsoft.EntityFrameworkCore.DbContext, Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.IDataProtectionKeyContext
    {
        public EntityFrameworkCoreXmlRepository(System.IServiceProvider services, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public virtual System.Collections.Generic.IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() { throw null; }
        public void StoreElement(System.Xml.Linq.XElement element, string friendlyName) { }
    }
    public partial interface IDataProtectionKeyContext
    {
        Microsoft.EntityFrameworkCore.DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys { get; }
    }
}
