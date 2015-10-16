// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class DatabaseErrorPageOptions
    {
        public virtual bool ShowExceptionDetails { get; set; }
        public virtual bool ListMigrations { get; set; }
        public virtual bool EnableMigrationCommands { get; set; }
        public virtual PathString MigrationsEndPointPath { get; set; }
    }
}
