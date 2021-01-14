// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    internal class DatabaseContextDetails
    {
        public Type Type { get; }
        public bool DatabaseExists { get; }
        public bool PendingModelChanges { get; }
        public IEnumerable<string> PendingMigrations { get; }

        public DatabaseContextDetails(Type type, bool databaseExists, bool pendingModelChanges, IEnumerable<string> pendingMigrations)
        {
            Type = type;
            DatabaseExists = databaseExists;
            PendingModelChanges = pendingModelChanges;
            PendingMigrations = pendingMigrations;
        }
    }
}
