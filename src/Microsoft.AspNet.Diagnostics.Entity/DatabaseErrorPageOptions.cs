// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class DatabaseErrorPageOptions
    {
        public static DatabaseErrorPageOptions ShowAll => new DatabaseErrorPageOptions
                                                              {
                                                                  ShowExceptionDetails = true,
                                                                  ListMigrations = true,
                                                              };

        public virtual bool ShowExceptionDetails { get; set; }

        public virtual bool ListMigrations { get; set; }
    }
}
