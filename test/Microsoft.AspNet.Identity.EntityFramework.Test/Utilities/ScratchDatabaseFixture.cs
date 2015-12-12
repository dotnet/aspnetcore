// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity.EntityFramework.Test.Utilities;
using Microsoft.Data.Entity.Internal;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    public class ScratchDatabaseFixture : IDisposable
    {
        private LazyRef<SqlServerTestStore> _testStore;

        public ScratchDatabaseFixture()
        {
            _testStore = new LazyRef<SqlServerTestStore>(() => SqlServerTestStore.CreateScratch());
        }

        public string ConnectionString => _testStore.Value.Connection.ConnectionString;

        public void Dispose()
        {
            if (_testStore.HasValue)
            {
                _testStore.Value?.Dispose();
            }
        }
    }
}