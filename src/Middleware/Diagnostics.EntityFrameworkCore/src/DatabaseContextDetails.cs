// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;

internal sealed class DatabaseContextDetails
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
